using System;
using System.Collections.Generic;
using System.Linq;
using SubpathGenerator;
using System.Threading.Tasks;
using System.Threading;
using GraphicGlobal;
using GraphicMinimal;

namespace Photonusmap
{
    //Erstellt N LightPaths und gibt das Ergebniss als Liste zurück
    public static class MultipleLightPathSampler
    {
        public static List<SubPath> SampleNLightPahts(ConstruktorDataForPhotonmapCreation data)
        {
            int[] photonCountArray = new int[data.ThreadCount];

            Task<List<SubPath>>[] tasks = new Task<List<SubPath>>[data.ThreadCount];
            for (int i = 0; i < tasks.Length; i++)
            {
                photonCountArray[i] = 0;

                //Wenn man mehrere Tasks starten will, wo sich bei den Start-Argumentan was unterscheidet, dann braucht man für all diese 'index'-Daten eine eigene Datenklasse
                tasks[i] = Task<List<SubPath>>.Factory.StartNew((object obj) =>
                {
                    //Hier ist die Thread-Funktion, welche bei Task.Wait/WaitAll gestartet wird
                    return SampleNLightPahts(data.LightPathSampler, data.LightPathCount / data.ThreadCount, photonCountArray, obj as ThreadIndexData, data.StopTrigger);
                }, new ThreadIndexData()
                {
                    //Die 'index'-Daten werden schon hier an der Stelle (in der for-Schleife) angelegt und dann später bei Task-WaitAll an den Task übergeben
                    Rand = new Rand((data.ThreadCount * (data.FrameId + 1)) + i),
                    Index = i
                }, data.StopTrigger.Token);
            }
            bool finish = false;
            do
            {
                data.ProgressChanged("Photonmap erstellen", photonCountArray.Sum() * 100.0f / data.LightPathCount);
                try
                {
                    finish = Task.WaitAll(tasks, 500);
                }
                //catch (OperationCanceledException)
                catch (System.AggregateException)
                {
                }
            } while (finish == false);

            return tasks.SelectMany(x => x.Result).ToList();
        }

        //Wenn man mehrere Tasks gleichzeitig startet, dann müssen all die Daten, welche sich bei jeden Tasks unterscheiden, in eine eigene Datenklasse ausgelagert werden
        class ThreadIndexData
        {
            public IRandom Rand;
            public int Index;
        }

        private static List<SubPath> SampleNLightPahts(SubpathSampler lightPathSampler, int photonCount, int[] photonCountArray, ThreadIndexData data, CancellationTokenSource stopTrigger)
        {
            List<SubPath> result = new List<SubPath>();
            for (int i = 0; i < photonCount; i++)
            {
                if (stopTrigger.IsCancellationRequested) return result;

                result.Add(lightPathSampler.SamplePathFromLighsource(data.Rand));
                photonCountArray[data.Index]++;
            }
            return result;
        }

        public static List<SubPath> SampleNLightPathsWithASingleThread(SubpathSampler lightPathSampler, int photonCount, IRandom rand, CancellationTokenSource stopTrigger)
        {
            List<SubPath> lightPaths = new List<SubPath>();
            for (int i = 0; i < photonCount; i++)
            {
                if (stopTrigger.IsCancellationRequested) return lightPaths;

                string randomObjectBase64Coded = rand.ToBase64String(); 

                try
                {
                    lightPaths.Add(lightPathSampler.SamplePathFromLighsource(rand));
                }catch (Exception ex)
                {
                    throw new RandomException(randomObjectBase64Coded, "SamplePathFromLighsource-Error", ex);
                }
                
            }
            //string allLightPahts = string.Join("\r\n", lightPaths.Select(x => x.ToPathSpaceString()));
            return lightPaths;
        }
    }
}
