using RayTracerGlobal;

namespace PointSearch
{
    class KdSplitResult
    {
        public int CuttingDimension;
        public float CuttingValue;
        public IPoint[] PoingsOnLeftSide;
        public IPoint[] PoingsOnRightSide;
    }

    //Gegeben ist Punktwolke 'points'
    //Diese Funktionssammlung sagt dann, auf welcher Axe die Punktwolke unterteilt wird
    //Das Ergebnis wird zurück gegeben, indem 'points' umsortiert wird so, dass alle pointsLeftSide-Punkte auf 
    //der linken Seite von cuttingValue liegen und der Rest auf der anderen Seite
    static class KdSplitFunctions
    {
        public delegate KdSplitResult SplittFunction(IPoint[] points, AxisAlignedBox boundingBox);

        private static float Error = 0.001f;    // a small value
        private static float MaximumAllowedAspectRationForFairSplit = 3.0f; // maximum allowed aspect ratio in fair split. Must be >= 2.

        private static float FindSpreadAlongGivenDimension(IPoint[] points, int dimension)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float f = points[i][dimension];
                if (f < min) min = f;
                if (f > max) max = f;
            }
            return max - min;
        }

        private static void FindMinMaxCoordinatesAlongDimension(IPoint[] points, int dimension, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float f = points[i][dimension];
                if (f < min) min = f;
                if (f > max) max = f;
            }
        }

        private static int FindDimensionOfMaxSpread(IPoint[] points, int dimension)
        {
            float maxSpread = float.MinValue;
            int maxDimension = -1;

            for (int d = 0; d < dimension; d++)
            {
                float spread = FindSpreadAlongGivenDimension(points, d);
                if (spread > maxSpread)
                {
                    maxSpread = spread;
                    maxDimension = d;
                }
            }

            return maxDimension;
        }

        //----------------------------------------------------------------------
        //	annMedianSplit - split point array about its median
        //		Splits a subarray of points pa[0..n] about an element of given
        //		rank (median: n_lo = n/2) with respect to dimension d.  It places
        //		the element of rank n_lo-1 correctly (because our splitting rule
        //		takes the mean of these two).  On exit, the array is permuted so
        //		that:
        //
        //		pa[0..n_lo-2][d] <= pa[n_lo-1][d] <= pa[n_lo][d] <= pa[n_lo+1..n-1][d].
        //
        //		The mean of pa[n_lo-1][d] and pa[n_lo][d] is returned as the
        //		splitting value.
        //
        //		All indexing is done indirectly through the index array pidx.
        //
        //		This function uses the well known selection algorithm due to
        //		C.A.R. Hoare.
        //----------------------------------------------------------------------
        private static void MedianSplitAboutGivenDimension(IPoint[] points, int dimensionAlongWhichToSplit, out float cuttingValue, out int pointsLeftSide)
        {
            Quicksort(points, dimensionAlongWhichToSplit, 0, points.Length - 1);

            pointsLeftSide = points.Length / 2;

            // cut value is midpoint value
            cuttingValue = (points[pointsLeftSide - 1][dimensionAlongWhichToSplit] + points[pointsLeftSide][dimensionAlongWhichToSplit]) / 2;
        }

        //----------------------------------------------------------------------
        //	annSplitBalance - compute balance factor for a given plane split
        //		Balance factor is defined as the number of points lying
        //		below the splitting value minus n/2 (median).  Thus, a
        //		median split has balance 0, left of this is negative and
        //		right of this is positive.  (The points are unchanged.)
        //----------------------------------------------------------------------
        private static int ComputeBalanceFactorForAGivenPlaneSplit(IPoint[] points, int dimensionAlongWhichToSplit, float cuttingValue)
        {
            int lessThanCuttingValueCounter = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i][dimensionAlongWhichToSplit] < cuttingValue) lessThanCuttingValueCounter++;
            }
            return lessThanCuttingValueCounter - points.Length / 2;
        }

        //----------------------------------------------------------------------
        //	kd_split - Bentley's standard splitting routine for kd-trees
        //		Find the dimension of the greatest spread, and split
        //		just before the median point along this dimension.
        //----------------------------------------------------------------------
        public static KdSplitResult MedianSplitFromLongestEdge(IPoint[] points, AxisAlignedBox boundingBox)
        {
            KdSplitResult result = new KdSplitResult();
            result.CuttingDimension = FindDimensionOfMaxSpread(points, boundingBox.Dimension);
            int pointsLeftSide;
            MedianSplitAboutGivenDimension(points, result.CuttingDimension, out result.CuttingValue, out pointsLeftSide);
            SplitArray(points, pointsLeftSide, out result.PoingsOnLeftSide, out result.PoingsOnRightSide);
            return result;
        }

        private static void Swap(IPoint[] points, int a, int b)
        {
            IPoint tmp = points[a];
            points[a] = points[b];
            points[b] = tmp;
        }

        private static void SplitArray(IPoint[] points, int pointsLeftSide, out IPoint[] pointsOnLeftSide, out IPoint[] pointsOnRightSide)
        {
            pointsOnLeftSide = new IPoint[pointsLeftSide];
            pointsOnRightSide = new IPoint[points.Length - pointsLeftSide];
            for (int i = 0; i < pointsOnLeftSide.Length; i++) pointsOnLeftSide[i] = points[i];
            for (int i = 0; i < pointsOnRightSide.Length; i++) pointsOnRightSide[i] = points[i + pointsLeftSide];
        }

        private static void Quicksort(IPoint[] points, int dimensionAlongWhichToSplit, int lo, int hi)
        {
            if (lo < hi)
            {
                int p = Partition(points, dimensionAlongWhichToSplit, lo, hi);
                Quicksort(points, dimensionAlongWhichToSplit, lo, p);
                Quicksort(points, dimensionAlongWhichToSplit, p + 1, hi);
            }
        }

        private static int Partition(IPoint[] points, int dimensionAlongWhichToSplit, int lo, int hi)
        {
            float pivot = points[lo][dimensionAlongWhichToSplit];
            int i = lo - 1;
            int j = hi + 1;
            while (true)
            {
                do i++; while (points[i][dimensionAlongWhichToSplit] < pivot);
                do j--; while (points[j][dimensionAlongWhichToSplit] > pivot);
                if (i >= j) return j;

                Swap(points, i, j);
            }
        }

        //Die Elemente im points-Array werden mit Quicksort sortiert (Das ist die Variante von Sunil Arya and David Mount aber sie funktioniert nicht)
        /*public static void MedianSplit(IPoint[] points, int startIndex, int numberOfPoints, int dimensionAlongWhichToSplit, out float cuttingValue, int pointsLeftSide)
        {
            int d = dimensionAlongWhichToSplit;

            int l = startIndex;                         // left end of current subarray
            int r = startIndex + numberOfPoints - 1;    // right end of current subarray
            while (l < r)
            {
                int i = (r + l) / 2;                    // select middle as pivot

                if (points[i][d] > points[r][d])        // make sure last > pivot
                {
                    Swap(points, i, r);
                }
                Swap(points, l, i);                     // move pivot to first position

                float c = points[l][d];                 // pivot value
                i = l;
                int k = r;
                for (; ; )                              // pivot about c
                {
                    while (points[++i][d] < c) ;
                    while (points[--k][d] > c) ;
                    if (i < k)
                        Swap(points, i, k);
                    else
                        break;
                }
                Swap(points, l, k);                     // pivot winds up in location k

                if (k > startIndex + pointsLeftSide)
                    r = k - 1;                          // recurse on proper subarray
                else if (k < startIndex + pointsLeftSide)
                    l = k + 1;
                else
                    break;
            }

            if (pointsLeftSide > 0)                     // search for next smaller item
            {
                float c = points[startIndex][d];        // candidate for max
                int k = startIndex;                     // candidate's index
                for (int i = startIndex + 1; i < startIndex + pointsLeftSide; i++)
                {
                    if (points[i][d] > c)
                    {
                        c = points[i][d];
                        k = i;
                    }
                }
                Swap(points, startIndex + pointsLeftSide - 1, k);    // max among points[startIndex..startIndex + pointsLeftSide - 1] to points[startIndex + pointsLeftSide - 1]
            }

            // cut value is midpoint value
            cuttingValue = (points[startIndex + pointsLeftSide - 1][d] + points[startIndex + pointsLeftSide][d]) / 2;
        }*/

        //----------------------------------------------------------------------
        //	midpt_split - midpoint splitting rule for box-decomposition trees
        //
        //		This is the simplest splitting rule that guarantees boxes
        //		of bounded aspect ratio.  It simply cuts the box with the
        //		longest side through its midpoint.  If there are ties, it
        //		selects the dimension with the maximum point spread.
        //
        //		WARNING: This routine (while simple) doesn't seem to work
        //		well in practice in high dimensions, because it tends to
        //		generate a large number of trivial and/or unbalanced splits.
        //		Either kd_split(), sl_midpt_split(), or fair_split() are
        //		recommended, instead.
        //----------------------------------------------------------------------
        public static KdSplitResult MidpointSplitFromLongestEdge(IPoint[] points, AxisAlignedBox boundingBox)
        {
            KdSplitResult result = new KdSplitResult();
            float longestBoxSide = boundingBox.GetLongestEdgeLength(); // find length of longest box side

            // find long side with most spread
            result.CuttingDimension = -1;
            float maxSpread = -1;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                if (boundingBox.GetEdgeLength(d) >= (1 - Error) * longestBoxSide)
                {
                    float spread = FindSpreadAlongGivenDimension(points, d);
                    if (spread > maxSpread)
                    {
                        maxSpread = spread;
                        result.CuttingDimension = d;
                    }
                }
            }

            // split along cut_dim at midpoint
            result.CuttingValue = (boundingBox.LowerBound[result.CuttingDimension] + boundingBox.UpperBound[result.CuttingDimension]) / 2;

            // permute points accordingly
            int br1, br2;
            PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
            //------------------------------------------------------------------
            //	On return:		pa[0..br1-1] < cut_val
            //					pa[br1..br2-1] == cut_val
            //					pa[br2..n-1] > cut_val
            //
            //	We can set n_lo to any value in the range [br1..br2].
            //	We choose split so that points are most evenly divided.
            //------------------------------------------------------------------
            int pointsLeftSide;
            if (br1 > points.Length / 2) pointsLeftSide = br1;
            else if (br2 < points.Length / 2) pointsLeftSide = br2;
            else pointsLeftSide = points.Length / 2;
            SplitArray(points, pointsLeftSide, out result.PoingsOnLeftSide, out result.PoingsOnRightSide);
            return result;
        }

        private static void PlaneSplit(IPoint[] points, int dimensionAlongWhichToSplit, float cuttingValue, out int valuesLessThanCuttingValue, out int valuesEqualCuttingValue)
        {
            int d = dimensionAlongWhichToSplit;
            int l = 0;
            int r = points.Length - 1;
            for (; ; )                          // partition pa[0..n-1] about cv
            {
                while (l < points.Length && points[l][d] < cuttingValue) l++;
                while (r >= 0 && points[r][d] >= cuttingValue) r--;
                if (l > r) break;
                Swap(points, l, r);
                l++; r--;
            }

            valuesLessThanCuttingValue = l;     // now: pa[0..br1-1] < cv <= pa[br1..n-1]
            r = points.Length - 1;
            for (; ; )                          // partition pa[br1..n-1] about cv
            {
                while (l < points.Length && points[l][d] <= cuttingValue) l++;
                while (r >= valuesLessThanCuttingValue && points[r][d] > cuttingValue) r--;
                if (l > r) break;
                Swap(points, l, r);
                l++; r--;
            }
            valuesEqualCuttingValue = l;        // now: pa[br1..br2-1] == cv < pa[br2..n-1]
        }

        //----------------------------------------------------------------------
        //	sl_midpt_split - sliding midpoint splitting rule
        //
        //		This is a modification of midpt_split, which has the nonsensical
        //		name "sliding midpoint".  The idea is that we try to use the
        //		midpoint rule, by bisecting the longest side.  If there are
        //		ties, the dimension with the maximum spread is selected.  If,
        //		however, the midpoint split produces a trivial split (no points
        //		on one side of the splitting plane) then we slide the splitting
        //		(maintaining its orientation) until it produces a nontrivial
        //		split. For example, if the splitting plane is along the x-axis,
        //		and all the data points have x-coordinate less than the x-bisector,
        //		then the split is taken along the maximum x-coordinate of the
        //		data points.
        //
        //		Intuitively, this rule cannot generate trivial splits, and
        //		hence avoids midpt_split's tendency to produce trees with
        //		a very large number of nodes.
        //
        //----------------------------------------------------------------------
        public static KdSplitResult SlidingMidpointSplitFromLongestEdge(IPoint[] points, AxisAlignedBox boundingBox)
        {
            KdSplitResult result = new KdSplitResult();
            float longestBoxSide = boundingBox.GetLongestEdgeLength(); // find length of longest box side

            // find long side with most spread
            result.CuttingDimension = -1;
            float maxSpread = -1;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                if (boundingBox.GetEdgeLength(d) >= (1 - Error) * longestBoxSide)
                {
                    float spread = FindSpreadAlongGivenDimension(points, d);
                    if (spread > maxSpread)
                    {
                        maxSpread = spread;
                        result.CuttingDimension = d;
                    }
                }
            }

            // ideal split at midpoint
            float idealCuttingValue = (boundingBox.LowerBound[result.CuttingDimension] + boundingBox.UpperBound[result.CuttingDimension]) / 2;

            // find min/max coordinates
            float min, max;
            FindMinMaxCoordinatesAlongDimension(points, result.CuttingDimension, out min, out max);

            if (idealCuttingValue < min)			// slide to min or max as needed
                result.CuttingValue = min;
            else if (idealCuttingValue > max)
                result.CuttingValue = max;
            else
                result.CuttingValue = idealCuttingValue;

            // permute points accordingly
            int br1, br2;
            PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
            //------------------------------------------------------------------
            //	On return:		pa[0..br1-1] < cut_val
            //					pa[br1..br2-1] == cut_val
            //					pa[br2..n-1] > cut_val
            //
            //	We can set n_lo to any value in the range [br1..br2] to satisfy
            //	the exit conditions of the procedure.
            //
            //	if ideal_cut_val < min (implying br2 >= 1),
            //			then we select n_lo = 1 (so there is one point on left) and
            //	if ideal_cut_val > max (implying br1 <= n-1),
            //			then we select n_lo = n-1 (so there is one point on right).
            //	Otherwise, we select n_lo as close to n/2 as possible within
            //			[br1..br2].
            //------------------------------------------------------------------
            int pointsLeftSide;
            if (idealCuttingValue < min) pointsLeftSide = 1;
            else if (idealCuttingValue > max) pointsLeftSide = points.Length - 1;
            else if (br1 > points.Length / 2) pointsLeftSide = br1;
            else if (br2 < points.Length / 2) pointsLeftSide = br2;
            else pointsLeftSide = points.Length / 2;
            SplitArray(points, pointsLeftSide, out result.PoingsOnLeftSide, out result.PoingsOnRightSide);
            return result;
        }

        //----------------------------------------------------------------------
        //	fair_split - fair-split splitting rule
        //
        //		This is a compromise between the kd-tree splitting rule (which
        //		always splits data points at their median) and the midpoint
        //		splitting rule (which always splits a box through its center.
        //		The goal of this procedure is to achieve both nicely balanced
        //		splits, and boxes of bounded aspect ratio.
        //
        //		A constant FS_ASPECT_RATIO is defined. Given a box, those sides
        //		which can be split so that the ratio of the longest to shortest
        //		side does not exceed ASPECT_RATIO are identified.  Among these
        //		sides, we select the one in which the points have the largest
        //		spread. We then split the points in a manner which most evenly
        //		distributes the points on either side of the splitting plane,
        //		subject to maintaining the bound on the ratio of long to short
        //		sides. To determine that the aspect ratio will be preserved,
        //		we determine the longest side (other than this side), and
        //		determine how narrowly we can cut this side, without causing the
        //		aspect ratio bound to be exceeded (small_piece).
        //
        //		This procedure is more robust than either kd_split or midpt_split,
        //		but is more complicated as well.  When point distribution is
        //		extremely skewed, this degenerates to midpt_split (actually
        //		1/3 point split), and when the points are most evenly distributed,
        //		this degenerates to kd-split.
        //----------------------------------------------------------------------
        public static KdSplitResult FairSplit(IPoint[] points, AxisAlignedBox boundingBox)
        {
            KdSplitResult result = new KdSplitResult();
            float longestBoxSide = boundingBox.GetLongestEdgeLength();      // find length of longest box side

            // find legal cut with max spread
            result.CuttingDimension = -1;
            float maxSpread = -1;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                // is this side midpoint splitable without violating aspect ratio?
                if (longestBoxSide * 2 / boundingBox.GetEdgeLength(d) <= MaximumAllowedAspectRationForFairSplit) //Wie wird hier Division durch Null verhindert?
                {
                    float spread = FindSpreadAlongGivenDimension(points, d);
                    if (spread > maxSpread)
                    {
                        maxSpread = spread;
                        result.CuttingDimension = d;
                    }
                }
            }

            // find longest side other than cuttingDimension
            float longestBoxSideOtherDimesion = float.MinValue;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                float length = boundingBox.GetEdgeLength(d);
                if (d != result.CuttingDimension && length > longestBoxSideOtherDimesion)
                {
                    longestBoxSideOtherDimesion = length;
                }
            }

            float smallPiece = longestBoxSideOtherDimesion / MaximumAllowedAspectRationForFairSplit;
            float lowestLegalCut = boundingBox.LowerBound[result.CuttingDimension] + smallPiece;   // lowest legal cut
            float highestLegalCut = boundingBox.UpperBound[result.CuttingDimension] - smallPiece;  // highest legal cut

            int br1, br2, pointsLeftSide;
            if (ComputeBalanceFactorForAGivenPlaneSplit(points, result.CuttingDimension, lowestLegalCut) >= 0)
            {
                result.CuttingValue = lowestLegalCut;
                PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                pointsLeftSide = br1;
            }
            else if (ComputeBalanceFactorForAGivenPlaneSplit(points, result.CuttingDimension, highestLegalCut) <= 0)
            {
                result.CuttingValue = highestLegalCut;
                PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                pointsLeftSide = br2;
            }
            else
            {
                MedianSplitAboutGivenDimension(points, result.CuttingDimension, out result.CuttingValue, out pointsLeftSide);
            }
            SplitArray(points, pointsLeftSide, out result.PoingsOnLeftSide, out result.PoingsOnRightSide);
            return result;
        }

        //----------------------------------------------------------------------
        //	sl_fair_split - sliding fair split splitting rule
        //
        //		Sliding fair split is a splitting rule that combines the
        //		strengths of both fair split with sliding midpoint split.
        //		Fair split tends to produce balanced splits when the points
        //		are roughly uniformly distributed, but it can produce many
        //		trivial splits when points are highly clustered.  Sliding
        //		midpoint never produces trivial splits, and shrinks boxes
        //		nicely if points are highly clustered, but it may produce
        //		rather unbalanced splits when points are unclustered but not
        //		quite uniform.
        //
        //		Sliding fair split is based on the theory that there are two
        //		types of splits that are "good": balanced splits that produce
        //		fat boxes, and unbalanced splits provided the cell with fewer
        //		points is fat.
        //
        //		This splitting rule operates by first computing the longest
        //		side of the current bounding box.  Then it asks which sides
        //		could be split (at the midpoint) and still satisfy the aspect
        //		ratio bound with respect to this side.	Among these, it selects
        //		the side with the largest spread (as fair split would).	 It
        //		then considers the most extreme cuts that would be allowed by
        //		the aspect ratio bound.	 This is done by dividing the longest
        //		side of the box by the aspect ratio bound.	If the median cut
        //		lies between these extreme cuts, then we use the median cut.
        //		If not, then consider the extreme cut that is closer to the
        //		median.	 If all the points lie to one side of this cut, then
        //		we slide the cut until it hits the first point.	 This may
        //		violate the aspect ratio bound, but will never generate empty
        //		cells.	However the sibling of every such skinny cell is fat,
        //		and hence packing arguments still apply.
        //
        //----------------------------------------------------------------------
        public static KdSplitResult SlidingFairSplit(IPoint[] points, AxisAlignedBox boundingBox)
        {
            KdSplitResult result = new KdSplitResult();
            float longestBoxSide = boundingBox.GetLongestEdgeLength();      // find length of longest box side

            // find legal cut with max spread
            result.CuttingDimension = -1;
            float maxSpread = -1;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                // is this side midpoint splitable without violating aspect ratio?
                if (longestBoxSide * 2 / boundingBox.GetEdgeLength(d) <= MaximumAllowedAspectRationForFairSplit) //Wie wird hier Division durch Null verhindert?
                {
                    float spread = FindSpreadAlongGivenDimension(points, d);
                    if (spread > maxSpread)
                    {
                        maxSpread = spread;
                        result.CuttingDimension = d;
                    }
                }
            }

            // find longest side other than cuttingDimension
            float longestBoxSideOtherDimesion = float.MinValue;
            for (int d = 0; d < boundingBox.Dimension; d++)
            {
                float length = boundingBox.GetEdgeLength(d);
                if (d != result.CuttingDimension && length > longestBoxSideOtherDimesion)
                {
                    longestBoxSideOtherDimesion = length;
                }
            }

            float smallPiece = longestBoxSideOtherDimesion / MaximumAllowedAspectRationForFairSplit;
            float lowestLegalCut = boundingBox.LowerBound[result.CuttingDimension] + smallPiece;   // lowest legal cut
            float highestLegalCut = boundingBox.UpperBound[result.CuttingDimension] - smallPiece;  // highest legal cut

            // find min/max coordinates
            float min, max;
            FindMinMaxCoordinatesAlongDimension(points, result.CuttingDimension, out min, out max);

            int br1, br2, pointsLeftSide;
            if (ComputeBalanceFactorForAGivenPlaneSplit(points, result.CuttingDimension, lowestLegalCut) >= 0) // is median below lo_cut?
            {
                if (max > lowestLegalCut)   // are any points above lo_cut?
                {
                    result.CuttingValue = lowestLegalCut;
                    PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                    pointsLeftSide = br1;   // balance if there are ties
                }
                else // all points below lo_cut
                {
                    result.CuttingValue = max;
                    PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                    pointsLeftSide = points.Length - 1;
                }
            }
            else if (ComputeBalanceFactorForAGivenPlaneSplit(points, result.CuttingDimension, highestLegalCut) <= 0) // is median above hi_cut?
            {
                if (min < highestLegalCut)  // are any points below hi_cut?
                {
                    result.CuttingValue = highestLegalCut;
                    PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                    pointsLeftSide = br2;   // balance if there are ties
                }
                else // all points above hi_cut
                {
                    result.CuttingValue = min;
                    PlaneSplit(points, result.CuttingDimension, result.CuttingValue, out br1, out br2);
                    pointsLeftSide = 1;
                }
            }
            else // median cut is good enough
            {
                MedianSplitAboutGivenDimension(points, result.CuttingDimension, out result.CuttingValue, out pointsLeftSide);
            }
            SplitArray(points, pointsLeftSide, out result.PoingsOnLeftSide, out result.PoingsOnRightSide);
            return result;
        }
    }
}
