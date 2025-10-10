using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PlateRecognation
{
    public class PlateCharacterVotingAggregator
    {
        private int _cameraId;


        static readonly string[] Charset34 = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "V", "Y", "Z", "NC" };


        public event EventHandler<PlateOCRResultEventArgs> PlateResultReady;
        public event EventHandler<PlateImageEventArgs> PlateImageReady;


        public PlateCharacterVotingAggregator() { }

        public PlateCharacterVotingAggregator(int cameraId) : this()
        {
            _cameraId = cameraId;
        }

        public void FinalizeAndEmit(ThreadSafeList<AhmetPlateResult> plateResults)
        {
            if (plateResults.Count > 0)
            {
                int L = plateResults.Max(a => a.m_characters.Count);

                const double BONUS_3of3 = 0.20;
                const double BONUS_2of3 = 0.08;
                const double EPS = 1e-9;


                var outChars = new string[L];
                var posConfs = new double[L];


                for (int i = 0; i < L; i++)
                {

                    var agg = new Dictionary<string, double>(Charset34.Length);

                    foreach (var c in Charset34)
                        agg[c] = 0.0;

                    var argmaxVotes = new Dictionary<string, int>();

                    foreach (var item in plateResults)
                    {
                        if (i >= item.m_characters.Count)
                            continue; // bu örnekte bu pozisyon yok, geç


                        var seg = item.m_characters[i];


                        foreach (var kv in seg.Confidance)
                            agg[kv.Item1] += kv.Item3;

                        //foreach (var (idx, p) in seg.Confidance)    // (int idx, double p)
                        //    agg[idx] += p;


                        // çoğunluk oyu için bu örnekteki en yüksek hangi karakter?
                        var bestKV = seg.Confidance.Aggregate((a, b) => a.Item3 > b.Item3 ? a : b);

                        string bestChar = bestKV.Item1;
                        argmaxVotes[bestChar] = argmaxVotes.TryGetValue(bestChar, out var cnt) ? cnt + 1 : 1;

                    }


                    // 3) Çoğunluk bonusu ekle
                    foreach (var kv in argmaxVotes)
                    {
                        if (kv.Value >= 3)
                            agg[kv.Key] += BONUS_3of3;
                        else if (kv.Value == 2)
                            agg[kv.Key] += BONUS_2of3;
                    }


                    // 4) En yüksek skorlu karakteri seç + pozisyon güvenini oranla hesapla
                    var best = agg.Aggregate((a, b) => a.Value > b.Value ? a : b);
                    outChars[i] = best.Key;

                    double denom = agg.Values.Sum() + EPS;
                    posConfs[i] = best.Value / denom; // "seçilen / toplam" = pozisyon güv.

                }



                // 5) Metni ve plaka güvenini üret
                string plate = string.Join("", outChars);

               
                double plateConf = posConfs.Average();

                PlateImageReady?.Invoke(this, new PlateImageEventArgs
                {
                    //Frame = plateResults[0].Img144x32.ToBitmap(),
                    //PlateImage = plateResults[0].readingPlateResult.ToBitmap(),


                    Frame = plateResults[0].plate,
                    PlateImage = plateResults[0].plate,

                    ReadingResult = plate,
                    Probability = plateConf
                });

                PlateResultReady?.Invoke(this, new PlateOCRResultEventArgs
                {
                    PlateText = plate,
                    DisplayDurationMs = 1000
                });
            }

        }
    }
}
