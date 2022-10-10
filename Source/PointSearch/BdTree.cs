using System;
using System.Collections.Generic;
using RayTracerGlobal;

namespace PointSearch
{
    //http://sarielhp.org/research/CG/collection/98/dist.pdf    -> Hier wird der BdTree erklärt

    //----------------------------------------------------------------------
    //	Box decomposition tree (bd-tree)
    //		The bd-tree is inherited from a kd-tree.  The main difference
    //		in the bd-tree and the kd-tree is a new type of internal node
    //		called a shrinking node (in the kd-tree there is only one type
    //		of internal node, a splitting node).  The shrinking node
    //		makes it possible to generate balanced trees in which the
    //		cells have bounded aspect ratio, by allowing the decomposition
    //		to zoom in on regions of dense point concentration.  Although
    //		this is a nice idea in theory, few point distributions are so
    //		densely clustered that this is really needed.
    //----------------------------------------------------------------------
    public class BdTree : KdTree, IPointSearch
    {
        public BdTree(IPoint[] points, int dimension, SplitRule splitRule = SplitRule.Suggest, ShrinkRule shrinkRule = ShrinkRule.Suggested, int maxPointCountPerLeafNode = 10)
        {
            this.points = points;
            this.boundingBox = new AxisAlignedBox(points, dimension);

            KdSplitFunctions.SplittFunction splitFunctionHandler = GetSplitHandler(splitRule);

            this.rootNode = BuildBdTreeRecursive(points, this.boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, shrinkRule, 15);
        }

        //----------------------------------------------------------------------
        //	Shrinking rules
        //----------------------------------------------------------------------
        public enum ShrinkRule
        {
            NoShrinking = 0,    // no shrinking at all (just kd-tree)
            SimpleSplitting = 1,
            CentroidSplitting = 2,
            Suggested = 3       // the authors' suggested choice
        }
        private enum DecompositionMethod { Split, Shrink };

        //----------------------------------------------------------------------
        //	trySimpleShrink - Attempt a simple shrink
        //
        //		We compute the tight bounding box of the points, and compute
        //		the 2*dim ``gaps'' between the sides of the tight box and the
        //		bounding box.  If any of the gaps is large enough relative to
        //		the longest side of the tight bounding box, then we shrink
        //		all sides whose gaps are large enough.  (The reason for
        //		comparing against the tight bounding box, is that after
        //		shrinking the longest box size will decrease, and if we use
        //		the standard bounding box, we may decide to shrink twice in
        //		a row.  Since the tight box is fixed, we cannot shrink twice
        //		consecutively.)
        //----------------------------------------------------------------------
        private const float GapThreshold = 0.5f;    // gap threshold (must be < 1)
        private const int MinNumberOfShrinkSides = 2;

        private static DecompositionMethod TryASimpleShrink(IPoint[] points,
                                                     AxisAlignedBox boundingBox,      // current bounding box
                                                     out AxisAlignedBox innerBox)     // inner box if shrinking (returned)
        {
            innerBox = new AxisAlignedBox(points, boundingBox.Dimension);

            float longestBoxSide = innerBox.GetLongestEdgeLength();

            int shrunkedSidesCounter = 0;
            for (int i = 0; i < boundingBox.Dimension; i++) // select which sides to shrink
            {
                float gapHighBetweenBoxes = boundingBox.UpperBound[i] - innerBox.UpperBound[i];

                if (gapHighBetweenBoxes < longestBoxSide * BdTree.GapThreshold)   // big enough gap to shrink?
                    innerBox.UpperBound[i] = boundingBox.UpperBound[i];     // no - expand
                else
                    shrunkedSidesCounter++;                                 // yes - shrink this side

                float gapLowBetweenBoxes = innerBox.LowerBound[i] - boundingBox.LowerBound[i];  // repeat for high side
                if (gapLowBetweenBoxes < longestBoxSide * BdTree.GapThreshold)
                    innerBox.LowerBound[i] = boundingBox.LowerBound[i];     // no - expand
                else
                    shrunkedSidesCounter++;                                 // yes - shrink this side
            }

            if (shrunkedSidesCounter >= BdTree.MinNumberOfShrinkSides)      // did we shrink enough sides?
                return DecompositionMethod.Shrink;

            return DecompositionMethod.Split;
        }

        //----------------------------------------------------------------------
        //	tryCentroidShrink - Attempt a centroid shrink
        //
        //	We repeatedly apply the splitting rule, always to the larger subset
        //	of points, until the number of points decreases by the constant
        //	fraction BD_FRACTION.  If this takes more than dim*BD_MAX_SPLIT_FAC
        //	splits for this to happen, then we shrink to the final inner box
        //	Otherwise we split.
        //----------------------------------------------------------------------
        private const float MaximumAllowedNumberOfSplits = 0.5f;    // maximum number of splits allowed
        private const float SplitFraction = 0.5f;                   // ...to reduce points by this fraction. This must be < 1.

        private static DecompositionMethod TryACentroidShrink(IPoint[] points,
                                                       AxisAlignedBox boundingBox,      // current bounding box
                                                       KdSplitFunctions.SplittFunction splitFunctionHandler,    // splitting procedure
                                                       out AxisAlignedBox innerBox)     // inner box if shrinking (returned)
        {
            int numberOfPointsInSubset = points.Length;
            int numberOfPointsInGoal = (int)(points.Length * BdTree.SplitFraction);
            int numberOfSplitsNeeded = 0;

            innerBox = boundingBox.GetCopy();

            while (numberOfPointsInSubset > numberOfPointsInGoal)   // keep splitting until goal reached
            {
                var splitResult = splitFunctionHandler(points, innerBox);
                numberOfSplitsNeeded++;                             // increment split count

                if (numberOfSplitsNeeded > boundingBox.Dimension * BdTree.MaximumAllowedNumberOfSplits) break;

                if (splitResult.PoingsOnLeftSide.Length >= numberOfPointsInSubset / 2) // most points on low side
                {
                    innerBox.UpperBound[splitResult.CuttingDimension] = splitResult.CuttingValue;   // collapse high side
                    points = splitResult.PoingsOnLeftSide;
                    numberOfPointsInSubset = points.Length;                   // recurse on lower points
                }
                else // most points on high side
                {
                    innerBox.LowerBound[splitResult.CuttingDimension] = splitResult.CuttingValue;   // collapse low side
                    points = splitResult.PoingsOnRightSide;
                    numberOfPointsInSubset = points.Length;
                }
            }

            if (numberOfSplitsNeeded > boundingBox.Dimension * BdTree.MaximumAllowedNumberOfSplits) // took too many splits
                return DecompositionMethod.Shrink;                                      // shrink to final subset

            return DecompositionMethod.Split;
        }

        //----------------------------------------------------------------------
        //	selectDecomp - select which decomposition to use
        //----------------------------------------------------------------------
        private static DecompositionMethod SelectDecompositionMethod(IPoint[] points,
                                                       AxisAlignedBox boundingBox,      // current bounding box
                                                       KdSplitFunctions.SplittFunction splitFunctionHandler,    // splitting procedure
                                                       ShrinkRule shrinkRule,
                                                       out AxisAlignedBox innerBox)     // inner box if shrinking (returned)
        {
            DecompositionMethod decomposition = DecompositionMethod.Split;

            switch (shrinkRule)                                 // check shrinking rule
            {
                case ShrinkRule.NoShrinking:                    // no shrinking allowed
                    decomposition = DecompositionMethod.Split;
                    innerBox = null;
                    break;
                case ShrinkRule.Suggested:
                case ShrinkRule.SimpleSplitting:
                    decomposition = TryASimpleShrink(points, boundingBox, out innerBox);
                    break;
                case ShrinkRule.CentroidSplitting:
                    decomposition = TryACentroidShrink(points, boundingBox, splitFunctionHandler, out innerBox);
                    break;
                default:
                    throw new Exception("ShrinkRule not available: " + shrinkRule.ToString());
            }

            return decomposition;
        }

        //points ist eine Punktliste und box liegt innerhalb der Bounding-Box von der Punktliste
        //Es wird die points-Liste in zwei Mengen unterteilet: Die Punkte, die innerhalb der box liegen und der Rest
        private static void SplitPointArrayByBox(IPoint[] points, AxisAlignedBox box, out IPoint[] pointsInsideTheBox, out IPoint[] pointsOutsideTheBox)
        {
            List<IPoint> inside = new List<IPoint>();
            List<IPoint> outside = new List<IPoint>();

            foreach (var point in points)
            {
                if (box.IsPointInside(point))
                    inside.Add(point);
                else
                    outside.Add(point);
            }

            pointsInsideTheBox = inside.ToArray();
            pointsOutsideTheBox = outside.ToArray();
        }

        private static AxisAlignedHalfspace[] ConvertBoundingBoxToListOfBounds(AxisAlignedBox innerBox, AxisAlignedBox enclosingBox)
        {
            List<AxisAlignedHalfspace> bounds = new List<AxisAlignedHalfspace>();

            for (int d = 0; d < innerBox.Dimension; d++)
            {
                if (innerBox.LowerBound[d] > enclosingBox.LowerBound[d])
                {
                    bounds.Add(new AxisAlignedHalfspace(d, innerBox.LowerBound[d], AxisAlignedHalfspace.Side.Right));
                }
                if (innerBox.UpperBound[d] < enclosingBox.UpperBound[d])
                {
                    bounds.Add(new AxisAlignedHalfspace(d, innerBox.UpperBound[d], AxisAlignedHalfspace.Side.Left));
                }
            }

            return bounds.ToArray();
        }

        //----------------------------------------------------------------------
        //	rbd_tree - recursive procedure to build a bd-tree
        //
        //		This is analogous to rkd_tree, but for bd-trees.  See the
        //		procedure rkd_tree() in kd_split.cpp for more information.
        //
        //		If the number of points falls below the bucket size, then a
        //		leaf node is created for the points.  Otherwise we invoke the
        //		procedure selectDecomp() which determines whether we are to
        //		split or shrink.  If splitting is chosen, then we essentially
        //		do exactly as rkd_tree() would, and invoke the specified
        //		splitting procedure to the points.  Otherwise, the selection
        //		procedure returns a bounding box, from which we extract the
        //		appropriate shrinking bounds, and create a shrinking node.
        //		Finally the points are subdivided, and the procedure is
        //		invoked recursively on the two subsets to form the children.
        //----------------------------------------------------------------------
        private static IKdNode BuildBdTreeRecursive(IPoint[] points, AxisAlignedBox boundingBox, int maxPointCountPerLeafNode, KdSplitFunctions.SplittFunction splitFunctionHandler, ShrinkRule shrinkRule, int rekursionDeep)
        {
            if (points.Length <= maxPointCountPerLeafNode || rekursionDeep < 0)   // n small, make a leaf node
            {
                return new KdLeafNode(points, boundingBox.Dimension);
            }

            //if (rekursionDeep < 0) throw new Exception("Rekursionsalarm: " + points.Length);

            AxisAlignedBox innerBox;
            DecompositionMethod decomposition = SelectDecompositionMethod(points, boundingBox, splitFunctionHandler, shrinkRule, out innerBox);

            if (decomposition == DecompositionMethod.Split) // split selected
            {
                var splitResult = splitFunctionHandler(points, boundingBox);

                // save bounds for cutting dimension
                float lv = boundingBox.LowerBound[splitResult.CuttingDimension];
                float hv = boundingBox.UpperBound[splitResult.CuttingDimension];

                boundingBox.UpperBound[splitResult.CuttingDimension] = splitResult.CuttingValue;    // modify bounds for left subtree
                IKdNode leftNode = BuildBdTreeRecursive(splitResult.PoingsOnLeftSide, boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, shrinkRule, rekursionDeep - 1);
                boundingBox.UpperBound[splitResult.CuttingDimension] = hv;  // restore bounds

                boundingBox.LowerBound[splitResult.CuttingDimension] = splitResult.CuttingValue;
                IKdNode rightNode = BuildBdTreeRecursive(splitResult.PoingsOnRightSide, boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, shrinkRule, rekursionDeep - 1);
                boundingBox.LowerBound[splitResult.CuttingDimension] = lv;  // restore bounds

                return new KdSplitNode(splitResult.CuttingDimension, splitResult.CuttingValue, lv, hv, leftNode, rightNode);
            }
            else  // shrink selected
            {
                IPoint[] pointsInsideTheBox, pointsOutsideTheBox;
                SplitPointArrayByBox(points, innerBox, out pointsInsideTheBox, out pointsOutsideTheBox);

                IKdNode inChild = BuildBdTreeRecursive(pointsInsideTheBox, innerBox, maxPointCountPerLeafNode, splitFunctionHandler, shrinkRule, rekursionDeep - 1);
                IKdNode outChild = BuildBdTreeRecursive(pointsOutsideTheBox, boundingBox, maxPointCountPerLeafNode, splitFunctionHandler, shrinkRule, rekursionDeep - 1);

                var bounds = ConvertBoundingBoxToListOfBounds(innerBox, boundingBox);

                return new BdShrinkNode(bounds, inChild, outChild);
            }
        }
    }
}
