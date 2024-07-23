using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Diagnostics.Metrics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Noggog;
using DynamicData;
using System.Diagnostics;
using System.Threading;
using Mutagen.Bethesda.Plugins.Records;

namespace Communism
{
    public class Program
    {

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "communism.esp")
                .Run(args);
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Stopwatch totalstopWatch = new Stopwatch();
            Stopwatch innerstopWatch = new Stopwatch();
            Stopwatch outerstopWatch = new Stopwatch(); 
            var loadorder = state.LoadOrder.PriorityOrder;
            int counter = 0;
            totalstopWatch.Start();
            TimeSpan ts_outer = TimeSpan.Zero;
            TimeSpan ts_inner = TimeSpan.Zero;

            foreach (var placedObject in state.LoadOrder.PriorityOrder.PlacedObject().WinningContextOverrides(state.LinkCache))
            {
                outerstopWatch.Start();
                //If already disabled, skip
                if (placedObject.Record.MajorRecordFlagsRaw == 0x0000_0800) continue;
                var placedObjectRec = placedObject.Record;
                if (placedObjectRec.EditorID == null)
                {
                    if (!placedObject.Record.Owner.TryResolve<IFactionGetter>(state.LinkCache, out var placedObjectfac)
                        && !placedObject.Record.Owner.TryResolve<INpcGetter>(state.LinkCache, out var placedObjectnpc)) {
                        continue;
                    }
                    innerstopWatch.Start();
                    IPlacedObject modifiedObject = placedObject.GetOrAddAsOverride(state.PatchMod);
                    modifiedObject.Owner.SetToNull();
                    innerstopWatch.Stop();

                }
                counter++;
                outerstopWatch.Stop();
                ts_inner += innerstopWatch.Elapsed;
                ts_outer += outerstopWatch.Elapsed - innerstopWatch.Elapsed;
                innerstopWatch.Reset();
                outerstopWatch.Reset();
            }
            totalstopWatch.Stop();
            TimeSpan tstotal = totalstopWatch.Elapsed;
            Console.WriteLine("placed objects = " +  counter);
            string elapsedTimeTotal = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",tstotal.Hours, tstotal.Minutes, tstotal.Seconds,tstotal.Milliseconds / 10);
            string elapsedTimeinner = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts_inner.Hours, ts_inner.Minutes, ts_inner.Seconds, ts_inner.Milliseconds / 10);
            string elapsedTimeouter = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts_outer.Hours, ts_outer.Minutes, ts_outer.Seconds, ts_outer.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTimeTotal);
            Console.WriteLine("RunTime outer loop " + elapsedTimeouter);
            Console.WriteLine("RunTime inner loop" + elapsedTimeinner);
            foreach (var cell in loadorder.Cell().WinningContextOverrides(state.LinkCache))
            {
                if(cell != null)
                {
                    if (!cell.Record.Owner.TryResolve<IFactionGetter>(state.LinkCache, out var placedObjectfac)
                        && !cell.Record.Owner.TryResolve<INpcGetter>(state.LinkCache, out var placedObjectnpc))
                    {
                        continue;
                    }
                    var overridenfac = cell.GetOrAddAsOverride(state.PatchMod);
                    overridenfac.Owner.SetToNull();
                }
            }

        }
    }
}
