using FullPathGenerator;
using FullPathGenerator.AnalyseHelper;
using FullPathGeneratorTest.BasicTests.BasicTestHelper;
using GraphicMinimal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubpathGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FullPathGeneratorTest.PixelRadianceTests
{
    //Hiermit möchte ich untersuchen, warum BPT ein zu dunkles Bild bei der Stilllifekerze erzeugt

    [TestClass]
    public class StilllifeCandleTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Wärend des Distanzsamples erstelle ich die Float-Zahl s und berechnet damit dann die PdfL
        //Wärend der Kontrolle berechne ich die Segmentlenge über RaxMax-RayMin. Dadurch kommt dann eine andere Distanz raus.
        //Deswegen kommt es dann zur Abweichung in der PdfL/EyePdfA
        //Die Get-CameraPdfW weicht auch leicht von der gesampelten PdfW ab was zu Fehlern in der EyePdfA führt.
        //Wenn ich den SizeFactor von der TestSzene auf 100 anstatt 10 stelle, dann läuft der Test ohne Probleme, da dann keine Division durch eine kleine QuadratDistanz erfolgt
        [TestMethod]
        public void A_Pathtracing_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene); 
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=1,37410765397056E-24}
        }

        //Auffälligkeit: VC hat ein größeren MaxError als PT. Bei den Standard-Media-A-Tests sind beide Verfahren gleich gut.
        //Die Frage ist also warum ist maxError bei VC bei dichten Medium größer?
        [TestMethod]
        public void A_VertexConnection_PathPdfA_PathContribution() //Test 1,2
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);
            var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000 ); //3.0e-05f, 3.0e-05f
            //maxError = {EyePdfA=0; LightPdfA=0; GeometryTerm=5,32974847326511E-20} Ohne Bug
            //maxError = {EyePdfA=1,97277262011511E-08; LightPdfA=5,39386286137996E-13; GeometryTerm=5,32974847326511E-20} Mit Bug (Wenn ich die PdfWReverse bei den Connectionpunkten nicht neu berechne)
        }

        //Hiermit teste ich den PdfATester, da PdfA-Fehler extrem schwer zu testen sind g=0.8 -> Fehler tritt auf
        [TestMethod]
        public void A_VertexConnectionWithError_g_0_8_PathPdfAContainsError() //Test 1,2
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnectionWithError);
            Exception exception = null;
            try
            {
                var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000); //3.0e-05f, 3.0e-05f
            }catch (Exception ex)
            {
                exception = ex;
            }
            Assert.IsNotNull(exception);
        }

        //Hiermit teste ich den PdfATester, da PdfA-Fehler extrem schwer zu testen sind g=0.0 -> Fehler tritt nicht auf obwohl ich die fehlerhafte Implementierung verwende
        [TestMethod]
        public void A_VertexConnectionWithError_g_0_0_PathPdfAContainsNoError() //Test 1,2
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 30, ScreenHeight = 30, PixX = 10, PixY = 10, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.0f });
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnectionWithError);
            Exception exception = null;
            try
            {
                var maxError = SinglePathCheck.ComparePathContributionWithGeometrySum(method, testSzene, 1000); //3.0e-05f, 3.0e-05f
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.IsNull(exception);
        }

        //Dieser Test zeigt, dass wenn man Anisotroph sampelt, dann ist die Radiance von PT+VC zu klein. PT, DL und LT stimmen.
        [TestMethod]
        public void PixelRadiance_PT_MatchWith_PTVC() //Test 7 (Pixelradiance)
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 6, ScreenHeight = 6, PixX = 2, PixY = 2, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f });
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,                
                //UseDirectLighting = true,                
                UseVertexConnection = true,
                //UseLightTracing = true,
            });

            //MaxPathLenght=80      Width=Height=30; PixX=PixY=10
            //        g=0.8   g=0
            //PT    = 599     92
            //PT+VC = 448     92

            //MaxPathLenght=20      Width=Height=30; PixX=PixY=10
            //        g=0.8   g=0       g=0.8 mit 1000000 samples
            //PT    = 216     9         214
            //PT+VC = 201     9         190
            //PT+DL = 217     9         213
            //LT    =         4         237

            // MaxPathLenght = 20   Width=Height=6; PixX=PixY=2
            //        g=0.8   g=0       
            //PT    = 119     3         
            //PT+VC = 94      3         
            //PT+DL = 119     3         
            //LT    = 117     2         

            // MaxPathLenght = 4   Width=Height=6; PixX=PixY=2          MaxPathLenght = 5                               MaxPathLenght = 6                               MaxPathLenght = 7                               MaxPathLenght = 8                               MaxPathLenght = 9                               MaxPathLenght = 10                              MaxPathLenght = 11                              MaxPathLenght = 12                              MaxPathLenght = 13                              MaxPathLenght = 14                              MaxPathLenght = 15                              MaxPathLenght = 16                              MaxPathLenght = 17                              MaxPathLenght = 18                              MaxPathLenght = 19                              MaxPathLenght = 20
            //        g=0.8                     g=0                     g=0.8                     g=0                   g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                     g=0.8                   g=0                                 
            //PT    = 0.0029525927734375        0.00583214794921875     0.076669445068359376      0.01167605078125      0.3025039306640625      0.026475906738281249    0.80705497753906252     0.076900843261718754    1.8162585202636719      0.16014934057617186     3.9282891022949218      0.20435944262695313     6.7339649375            0.28909385302734375     11.953125324707031      0.43565690161132814     18.471530038330076      0.53787460717773439     26.79212319165039       0.85243367431640626     36.548908766357421      0.97329172485351567     47.902469616943357      1.2793857495117187      61.034457446044925      1.5955054294433593      75.532968056396484      2.0734197792968749      89.316387034423826      2.3695963034667971      104.03275441894532      2.8282368706054686      119.4478799909668       3.1292540952148435
            //PT+VC = 0.010183290292610383      0.006177675459292366    0.0592341950884053        0.019828378654607386  0.26158266015665416     0.039819512166648245    0.81624042222537407     0.08501441113961164     2.0262238757698126      0.11859787587652115     3.9487234324379461      0.20200195463199294     7.174429826878665       0.34305576765288226     11.719192206807968      0.45885748929093007     18.447736854160464      0.55696265943521572     25.696643984621144      0.76413827677206425     34.980993185046778      0.98318012237340457     45.019502431391004      1.2511491920735647      56.86233731794168       1.5355419967161459      69.587362681075746      1.8915694690969385      79.796585493918414      2.2485624631623033      94.880282994606048      2.735849012604668       107.74990571814068      3.1141679848699289
            //PT+DL = 0.011079580504212285      0.0079152818295165645   0.067560730556654042      0.023170757604792348  0.2229816970146434      0.042185820944237576    0.78216790017542992     0.086795320705585807    1.8283935448365347      0.12871297741433119     3.6674008966220542      0.22627809461052686     6.92644326507998        0.29334136860125692     11.791518368725901      0.42097787244873813     18.30887343184002       0.57761111403639986     26.585599842446413      0.74413334614436821     37.277193434754125      0.94305339752475659     48.463303163099553      1.2498367862626352      61.016117812031865      1.5587470623450006      74.233119769450781      1.8430929693296205      88.8482174247317        2.2002956962280971      103.46623079014304      2.60688595944002        119.75740430805105      3.2674266218728572
            //LT    = 0.03763565149459084       0.0046179080407909049   0.0739620080411795        0.017329276717095412  0.12043117605281282     0.017442703730143113    0.42547681544493093     0.085506028640243426    1.5843098299703864      0.059258638301838244    4.7977195548487765      0.20725868155269234     5.9381782155809777      0.27901593229963212     10.32092849636763       0.47994995168256238     20.405376644104269      0.58677106775482635     24.95967213273012       0.70990168704744339     37.514575926875935      0.93313726652032991     47.502169899961139      1.2172809073304289      59.509599739663287      1.4586236656861995      80.840945708287833      2.0539826465942093      87.286937265680152      2.2763723179367887      105.07849932844282      2.3597534703671497      117.24451744281561      2.6522664839777788

            int actual = (int)PathContributionCalculator.GetMisWeightedPixelRadiance(testSzene, fullPathSampler, 10000);
            //double actual = PathContributionCalculator.GetMisWeightedPixelRadiance(testSzene, fullPathSampler, 100000);

            
            Assert.AreEqual(114, actual, $"Expected=118; Actual={actual}");//Bei 100.000 Samples muss 119 raus kommen; Bei 10.000 114
        }


        //Mit diesen Test kann ich die Radiance(und andere Pfad-Propertys) aufgesplittet nach Pfadlänge für alle möglichen Sampling-Verfahren sehen
        //Verwende ich alle VC-Sampler, dann ist die Radiance zu niedrig. Verwende ich nur ein einzelnen VC-Sampler, dann stimmt die Radiance.
        [TestMethod]
        [Ignore]
        public void PixelRadiance_CreateForAllCombinations() //Test 7 (Pixelradiance)
        {
            //var testSzene = new BoxTestSzene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = true, ScreenWidth = 6, ScreenHeight = 6, PixX = 2, PixY = 2, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f }); //MediaWürfel
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = false, CreateWalls = false, ScreenWidth = 3, ScreenHeight = 3, PixX = 1, PixY = 1, MaxPathLength = 20, ScatteringFromGlobalMedia = 5, AnisotrophyCoeffizient = 0.8f }); //GlobalMedia
            //var testSzene = new BoxTestSzene(new BoxData() { EyePathSamplingType = PathSamplingType.WithPdfAAndReversePdfA, LightPathSamplingType = PathSamplingType.WithPdfAAndReversePdfA, CreateMediaBox = false, ScreenWidth = 6, ScreenHeight = 6, PixX = 2, PixY = 2, MaxPathLength = 20, ScatteringFromMedia = 15f, AnisotrophyCoeffizient = 0.8f, SurfaceAlbedo = 1, WaendeMaterial = BrdfModel.Glossy }); //Ohne Media
            var fullPathSampler = PathContributionCalculator.CreateFullPathSampler(testSzene, new FullPathSettings()
            {
                UsePathTracing = true,
                UseDirectLighting = true,                
                UseVertexConnection = true,
                UseLightTracing = true,
            });

            //VC-All ist falsch; VC-Single kombiniert mit PT ist richtig. Nehme ich 9 VC-Sampler oder mehr, dann ist VC falsch. Verwende ich nur 1 bis 8 VC-Sampler, dann ist VC+PT richtig.
            //Radiance bei PathLength=20       VC(i=9,j=9)          VC(i=8,j=10)        VC(i=10,j=8)            VC(All i=1..17)     VC(i=7..12)   7     VC(i=2..16)  15     VC(i=3..15)   13    VC(i=4..14)   11    VC(i=5..13)    9    VC(i=6..12)   7     VC(i=6..13)    8    VC(i=1..8)    8     VC(i=10..17)   8    VC(i=1..7)     7    VC(i=1..6)     6    VC(i=1..5)     5
            //Pathtracing                      15,8495231201172     15,8495231201172    15,8495231201172        15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172    15,8495231201172
            //VertexConnection                 3,85386512791805     18,3410953042206    7,4892051867287         11,56426891267      19,7974317396008    11,5739782647462    12,0509161583722    14,9811704671039    19,3364954958106    20,7875113936978    19,8730838013071    12,7433775016085    11,1031499725271    12,5792964927811    12,2076821819375    11,9519113702794
            //Pathtracing + VertexConnection   15,6662989231437     15,4759008987911    15,9785511647593        12,6784128114451    15,1438611128029    12,9192648899172    13,3917376866654    13,6769117739547    14,6565290705174    15,2783022092254    15,1406277829388    14,6557674712231    14,7970165722319    14,9378103798905    14,9349898408651    15,1198123877728
            //Pathtracing + DirectLighting     15,6301628602406     15,6301628602406    15,6301628602406        15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406    15,6301628602406
            //LightTracing                     13,5371056659482     13,5371056659482    13,5371056659482        13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482    13,5371056659482

            //VC-All+PT PathLength=20
            //PathContribution mit Float: 12,6784128114451
            //PathContribution mit Double 12,678412786895

            //-Wenn ich das Medium dichter mache, hat das kein Einfluß auf den Fehler
            //-Wenn ich ohne Media das Licht gegen diffuse Wände 20 mal reflektiere, dann gibt es kein Fehler
            //-Wenn ich Width=Height=3;Pix=1 mache, dann wird der Fehler kleiner weil ich vermutlich direkter ins Licht schaue und somit die GetBrdf von der Phasenfunktion höhere Werte liefert
            //-Interessante Beachbachtung: Das Durchschnittliche Mis-Gewicht für Pfade der Länge 20 ausgesplittet nach FullpathSampler:
            //  VC(i=6..13)     = VC=0,992|PT=0,57       -> Radiance=15,1406277829388 (Referenzwert=15,6301628602406)
            //  VC(All i=1..17) = VC=0,987|PT=0,325      -> Radiance=12,6784128114451
            //  VC(i=1..6)      = VC=0,992|PT=0,625      -> Radiance=14,9349898408651
            // -> Ein von ein VC-Sampler erzeugter Pfad hat ein hohes MIS-Gewicht. Da nur beim Connection-Schritt der Pfad eine niedrige PfadPdfA hat, behauptet er sich gegen alle anderen Verfahren
            // -> Bei VC-All liegt das durchschnittliche MIS-Gewicht der Pfade, die per PT erzeugt wurden, niedriger als bei VC(6..13). 
            // -> Aber selbst wenn ich PT mit MIS 0.625 drin habe, kann ich noch immer zu niedrige Radiane haben. PT-MIS-Anteil alleine ist keine Erklärung.
            //-Dieses kleinere PT-MIS-Gewicht bei VC-All spiegelt sich dann auch in der RadiancePerSampler-Verteilung wieder:
            //  VC(i=6..13)     = VC=40|PT=59   -> PT-Anteil ist hier höher
            //  VC(All i=1..17) = VC=59|PT=40   -> Als hier.
            //  VC(i=1..6)      = VC=33|PT=66
            //-Die PowerHeuristic bei der MIS-Formel hilft nicht
            //-Die Summe aller Radiance-Werte über alle Pfade bei VC-All ist ja zu niedrig. Interessant ist auch das der MaxRadiance-Wert dort kleiner ist. AvgContribution ist gleich.
            //-Wenn die Radiance zu niedrig ist, dann muss ja entweder die PathContribution oder das Mis-Gewicht zu niedrig sein. Beide Werte einzeln betrachtet sehen
            // ok aus aber das Produkt daraus ist zu niedrig. 
            //15.9.2021: Wenn ich nur ein VC-Sampler verwende, dann sehe ich beim PixelRadiance_CreateForAllCombinations-Test für z.B. die Pfadlänge 20,
            //dass das Ergebnis stimmt. Verwende ich alle VC-Sampler, dann ist VC zu dunkel.
            //Vermutete Antwort: Die einzelnen VC-Sampler korrelieren. Weil sie alle aus den gleichen Eye- und Light-Subpfad erstellt wurden.
            //Dieser Satz von Eric Veach könnte mich bestätigen: Forschungen\Wintersemeser_2016_2017_Santa_Monica\Veach BDPT.pdf Seite 11
            //Note that the samples in each group are dependent (since they are all generated from the same light and eye subpath). However this does not significantly
            //affect the results, since the correlation between them goes to zero as we increase the number of independent sample groups for each measurement. For
            //example, if N independent light and eye subpaths are used, then all of the samples from each p_st are independent, and each sample from p_st is
            //correlated with only one of the N samples from any other given technique p_st'. From this fact it is easy to show that the variance results of Chapter 9
            //are not affected by more than a factor of (N-1)/N due to the correlation between samples in each group.
            //Frage wenn es denn an der Korrelation liegt: Warum passiert der Fehler dann nicht bei g=0?
            //19.9.2021: Die AvgPathPdfA bei VC ist bei g=0 AvgPdfA=2,18330578139731E-10 und bei g=0.8 AvgPdfA=3590,2104454128
            // Außerdem stelle ich hier fest, dass Korrlation bei traditionellen MIS-Gewichtsberechung für BPT nicht beachtet wird: https://onlinelibrary.wiley.com/doi/full/10.1111/cgf.142628 Forschungen\ParticipatingMedia_2019\Correlation-Aware Multiple Importance Sampling for Bidirectional Rendering Algorithms - Grittmann 2021.pdf
            // Könnte der VC-Fehler durch korrelierte Pfade entstehen und aus ein mir noch unbekannten Grund läßt eine hohe PfadPdfA die Korrelation stärker stören als bei niedriger PfadPdfA?
            //Weite Feststellung: SmallUPBP verwendet für jeden Eye-Subpath-Point ein anderen Light-Subpath, mit dem es dann lauter Verbindungen herstellt. Deswegen hat es das Korrelationsproblem wohl nicht.             
            //Siehe UPBP.hxx Zeile 1154 -> int pathIdxMod = mRng.GetUint() % pathCountL;    pathIdxMod ist der SubPath-Index aus dem Array über alle Light-Subpaths
            //20.9.2021: Ich habe nun auch für jeden Eye-Subpath-Point ein neuen Lightpath erzeugt aber das Fehlerbild war das gleiche. Dann habe ich für
            //jeden Eye- UND Light-Subpath-Point ein neuen Subpath erzeugt und der Fehler war auch noch gleich. D.h. nur weil ich bei VertexConnection für alle Subpath-Kombinationen
            //die gleichen Eye- und Lightsubpahts nehme, erzeugt das anscheinend KEIN Korrelationsfehler.
            //21.9.2021: Ohne MIS bei einer Million samples scheint die Radiance-Grafik zwar besser aus zu sehen aber die Radiane-Summe hat noch immer den Fehler
            // -Wenn ich ohne Media mit Glossy-Wänden arbeite, kommt der Fehler auch -> Vergleiche g=0.8 mit Glossy-NoMedia
            // -Die AvgPathPdfA ist nur deswegen bei g=0.8 so hoch, weil es 3 extreme Ausreißer gibt -> Wie sieht die AvgPdfA ohne die Ausreiße aus? Wie viel Ausreißer gibt es pro Pfadlänge?
            //22.9.2021: Wenn ich Die AvgPdfA-OhneFiryflys zwischen VC und PT+DL vergleiche, dann ist sie bei VC bedeutend größer. Warum?
            // -> Immer wenn eine hohhe PdfW (Strahl geht geradeaus) und danach eine kurze Distanz (Division durch kleines r²) dann erhält man eine
            // hohe PdfA. Normalerweise sind die nächsten 1-2 PdfA-Faktoren dann wieder < 1, wodurch sich dann solche Ausreißer dann weg machen.
            // Bei VC connecte ich dann auf jeden Partikel und somit nehme ich auch jeden Ausreißer mit. Bei PT/DL bekomme ich nur dann ein Ausreißzer,
            // wenn ich beim letzten/vorletzten Pfadpunkt mal eine hohe PdfA habe. 
            //Die Contribution ist bei VC so hoch, da die Light-Subpfade mit einer so hellen Lampe strahlen und das
            //Pfadgewicht bei Eye- als auch Light-Pfaden immer nahe 1 ist. D.h. ich schaue quasi "direkt" in die Lampe.
            //Bei DL ist die Contribution nur dann hoch, wenn der Eye-Point in der nähe der Lichtquelle liegt und die Brdf nicht zu niedrig ist.
            //Ansonsten verhindert der GeometryTerm und die Brdf zu hohe Contributionwerte bei DL. 
            //-Wenn ich die Pfad-Count von DL und all den einzelnen VC-Sampler ansehe dann sehe ich, dass es mit aufsteigenden VC-i immer mehr Pfade
            //gibt. Warum? -> Bei kleinen i-Index ist ein hoher j-Index nötig. Die LightSub-Pfade enden entweder wegen Absorbation am MediaPartikel
            //(20.000 bei Pfadlänge 20) oder weil sie auf die Lichtquelle treffen. Lichtpfade treffen vermutlich eher mal die Lichtquelle und sind
            //somit kürzer als Eye-Pfade. Deswegen ist es eher Wahrscheinlich ein langen Eye-Pfad zu haben als ein langen Light-Pfad. Deswegen hat
            //man mehr vc20Counter_i-Counter mit hohen Index als vc20Counter_j mit hohen.
            //Bei g=0.8
            // +AvgEyeLength = 17.93399
            // +AvgLightLength = 15.19327
            // +dl20Counter = 78344
            // +vc20Counter = [0 67500 67767 68107 68467 68893 69339 69724 70034 70450 70815 71246 71854 72673 73797 75353 77319 79585 0 0]
            // +vc20Counter_i = [0 100000 98986 98008 97039 96085 95020 93817 92544 91135 89599 88031 86545 84975 83507 82153 80830 79585 0 0]
            // +vc20Counter_j = [0 67500 68452 69493 70566 71723 72988 74335 75722 77335 79075 81009 83089 85594 88406 91739 95677 100000 0 0]
            // +lightPathHitLightsCounter = 19907
            //Bei g=0.0
            // +AvgEyeLength = 18.35937
            // +AvgLightLength = 10.33494
            // +dl20Counter = 83874
            // +vc20Counter = [0 36786 37306 37828 38550 39317 40129 41105 42151 43333 44715 46501 48591 51474 55224 60515 68958 84775 0 0]
            // +vc20Counter_i = [0 100000 99067 98055 97083 96123 95165 94141 93195 92245 91287 90336 89395 88452 87530 86595 85689 84775 0 0]
            // +vc20Counter_j = [0 36786 37634 38556 39664 40845 42089 43594 45201 46970 48977 51440 54307 58137 63071 69838 80451 100000 0 0]
            // +lightPathHitLightsCounter = 55158
            //23.9.2021: Das Durchschnittliche DL-Connectiongewicht ist 10 mal größer als das von VC, da bei DL die LightBrdf nicht mit dabei ist
            //-Wenn ich PT mit DL und PT mit allen VC-Einzelsamplern kombiniere, dann stimmen alle VC-Sampler
            //-Wenn ich die VC-PdfA durch die Pfadlänge, oder sogar durch die Quadrat-Pfadlänge dividiere, dann ist VC-All+PT besser/richtig aber VC-Single+PT zu niedrig 
            //-Der g=0.8-Term führt dazu, dass die Phasenfunktion nur bei kleinen Winkelbereichen eine große PdfW hat. Das gleiche passiert
            // auch bei Glossy-Brdfs ohne Media. Wenn man so eine Brdf/Phasenfunktion hat, dann muss man mit Brdf-Sampling/Pathtracing die
            // Fullpaths erzeugen. 
            //-Wenn ich bei der g=0.8-Umgebung mit VC ein Pfad erzeuge, dann ist sowohl die PathContribution als auch PfadPdfA wegen der kleinen
            // Eye- und Light-Brdf/PdfW-Werte gering. Deswegen hat VC eine hohe Varianz.
            //-Wenn ich bei der g=0.8-Umgebung mit PT ein Pfad erzeuge, dann ist wegen des ImportanceSamplings von der Phasenfunktion die PdfA
            // und PathContribution hoch. Die GetPdf vom VC wird für so ein Pfad nicht so hoch sein, wenn das letzte Pfadstück, was auf die Lichtquelle
            // trifft nicht gerade sehr kurz ist. 
            //-Frage: Wenn ich PT nur mit ein einzelnen VC-Sampler kombiniere, dann bekommen die PT-Gesampelten Pfade ein hohes MIS-Gewicht.
            //        Wenn ich aber alle VC-Sampler nutze, dann ist das PT-Mis-Gewicht zu niedrig. Frage ist nun: Ist es die Summe über alle 
            //        VC-GetPdfs, welche dann im Verhältnis zur PT-Pdf zu hoch werden oder ist einer von den VC-Samplern dabei, welcher für die 
            //        zu hohe VC-Pdf-Summe verantwortlich ist? -> Antwort: Meist sind immer 1 bis 3 VC-Sampler dabei, die eine deutliche höhere Pdf 
            //        haben als PT. 
            //-Wenn ich PT mit allen VC-Samplern kombiniere, dann sind immer 1-3 VC-Sampler dabei, die eine 10 bis 100 mal so hohe Pdf haben, wie PT.
            // Deswegen haben die Pfade, die PT sampelt eine zu niedriges MIS-Gewicht. Deswegen ist die Radiance so niedrig.
            // Bei g=0.8 hat man ähnlich wie bei Glossy-Brdfs die Besonderheit, das man Pfadtracing unbedingt braucht, um Varianzfrei genug sampeln zu können. 
            // Das Licht bewegt sich bei ein g=0.8-Medium quasi wie durch ein dünnen Schlauch und PT wandert Dank Importancesampling der Phasenfunktion
            // in diesen Schlauch. VC müsste schon die Eye- und Light-Subpoints genau so samplen, dass der Pfad möglichst eine gerade Linie ergibt.
            //-Das einer von den VC-Samplern eine so hohe Pdf für ein PT-Pfad angibt kommt sowohl bei g=0.0 als auch g=0.8 vor. Bei g=0.8 stört das aber nicht
            // da die Radiance eh zum Großteil über VC bestimmt wird. 
            //-Wenn bei g=0.8 es immer mal paar einzelne VC-Sampler gibt, die meinen eine deutliche höhere PdfA zu haben, müssten dann nicht 1 bis 3 mal so
            // viele VC-Samples mit entsprechend hoher PdfA zu finden sein? -> Ja es gibt deutlich mehr VC-Samples mit höherer PdfA.
            // Nur ist die PathContribution von den VC-Samples auch deutlich niedriger als bei den PT-Pfaden, da beim VC-Connection-Stück die ganze
            // Radiance verloren geht. Der Endpunkt vom LightSubpfad hat ein sehr hohes Pfadgewicht so wie auch der Endpoint vom EyeSubPath.
            //-Wenn ich beim g=0.8-Medium die Eye- und Light-Brdf vom VC-Connection-Schritt auf 1 setze, dann erzeugt VC auch Pfade mit fast so 
            // hoher Contribution wie PT. Nur die Attenuation schwächt das noch ab, wenn die Verbindungspunkte ein zu großen Abstand haben. 
            //24.9.2021: Die Radiance bei g=0.8-Media ist zu niedrig, da das MIS-Gewicht für PT-Gesampelte Pfade zu niedrig ist. Es ist in der Tat
            //so, das VC-Pfade ein hohe PdfA und eine niedrige Contribution haben. Immer dann, wenn PT eine lange Distanz sampelt oder um die Ecke
            //fliegt (Niedrige PdfW) dann ist einer von den VC-Samplern dann besser darin diesen Pfad zu erzeugen, da hohe Distanzen/Umknickpunkte
            //beim VC-Connectionschritt ohne Sampling gemacht werden. Da beim VC-Connection kein Importancesampling erfolgt, ist die Contribution
            //wegen niedrdiger Attenuation/PhasenBrdf beim Connectionstück dann insgesamt niedrig. Wenn ich nur einzelne VC-Sampler mit PT verbinde,
            //dann stimm das Ergebnis, was zeigt, dass die VC-Contribution richtig ist und auch die VC-PdfA wird dort nicht zu hoch angegeben.
            //Nur wenn ich alle VC-Sampler+PT verwende, dann ist die GetPdf-Abfrage von VC für PT-gesampelte Pfade zu hoch. 
            //Wenn denn mein PT-MIS-Gewicht zu niedrig ist, warum ist dann die Summe über PT-MIS-Gewichte beim Referenzalgorithmus gleich hoch wie bei mir?

            _ = PerPathLengthAnalyser.GetDataPerPathLengthFromMultipleCombinations(testSzene, fullPathSampler.SamplingMethods, 100000,
                new PerPathLengthAnalyser.CombinationData[]
                {
                    new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.PathTracing}},                                  //PT
                    new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.DirectLighting}},                               //DL
                    new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.VertexConnection}},                             //VT
                    new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.PathTracing, SamplingMethod.VertexConnection}}, //PT+VC
                    new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.PathTracing, SamplingMethod.DirectLighting}},   //PT+DL
                    //new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.PathTracing, SamplingMethod.DirectLighting, SamplingMethod.VertexConnection}},   //PT+DL+VT
                    //new PerPathLengthAnalyser.CombinationData(){Sampler = new SamplingMethod[]{ SamplingMethod.LightTracing}},                                 //LT
                });
        }

        //Hier sieht man, dass der PathSpace von VC meistens zu niedrig ist aber manchmal auch zu hoch
        [TestMethod]
        public void PathSpace_PT_MatchWith_VC() //Test 5
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = false, CreateWalls = false, ScreenWidth = 3, ScreenHeight = 3, PixX = 1, PixY = 1, MaxPathLength = 20, ScatteringFromGlobalMedia = 5, AnisotrophyCoeffizient = 0.8f }); //GlobalMedia
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.VertexConnection);

            var expected = new PathContributionForEachPathSpace(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceStilllifeKerze.txt");
            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck  / 2);

            string error = expected.CompareAllExceptExcludetPathsWithOther(actual, testSzene.maxContributionError, new string[] { "C P L", "C P P P P P P P P P P P P L" });
            Assert.AreEqual("", error, error);

        }

        [TestMethod]
        [Ignore]
        public void PathSpace_CreateExpectedValues() //Test 5
        {
            var testSzene = new BoxTestScene(new BoxData() { EyePathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, LightPathSamplingType = PathSamplingType.ParticipatingMediaShortRayWithDistanceSampling, CreateMediaBox = false, CreateWalls = false, ScreenWidth = 3, ScreenHeight = 3, PixX = 1, PixY = 1, MaxPathLength = 20, ScatteringFromGlobalMedia = 5, AnisotrophyCoeffizient = 0.8f }); //GlobalMedia
            var method = new PathSamplerFactory(testSzene).Create(SamplerEnum.Pathtracing);

            var actual = PathContributionCalculator.GetPathContributionForEachPathSpace(method, testSzene, testSzene.SamplecountForPathContributionCheck * 10);
            File.WriteAllText(WorkingDirectory + "ExpectedValues\\ExpectedValuesForPathSpaceRadianceStilllifeKerze.txt", actual.ToString());
        }

    }
}
