using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlateRecognation
{
    internal class PlateFormatHelper
    {
        public static bool IsPlatePatternValid(string plate)
        {
            // Basit Türk plakası yapısı kontrolü
            // Örneğin: 34ABC123, 06AB1234 gibi
            return System.Text.RegularExpressions.Regex.IsMatch(plate, @"^[0-9]{2}[A-ZÇĞİÖŞÜ]{1,3}[0-9]{2,4}$");
        }

        public static bool IsTurkishPlatePatternValid(string plate)
        {
            string desensayı = "[0-9]";
            string desenyazı = "[A-ZÇĞİÖŞÜ]";
            string sehir = "";
            string arayazi = "";
            string sonsayi = "";

            for (int i = 0; i < plate.Length; i++)
            {
                Match eslesme = Regex.Match(plate[i].ToString(), desensayı, RegexOptions.IgnoreCase);

                if (eslesme.Success)
                {
                    if (arayazi.Length <= 0)
                        sehir += eslesme.Value.ToString();
                    else
                        sonsayi += eslesme.Value.ToString();
                }
                else
                {
                    Match eslesme3 = Regex.Match(plate[i].ToString(), desenyazı, RegexOptions.None);

                    if (eslesme3.Success)
                        arayazi += eslesme3.Value.ToString();
                }
            }

            if (int.TryParse(sehir, out int city))
            {
                int middle = arayazi.Length;
                int last = sonsayi.Length;

                if (city >= 1 && city <= 81)
                {
                    if (middle == 1 && (last >= 4 && last <= 5))
                        return true;
                    if (middle == 2 && (last >= 3 && last <= 4))
                        return true;
                    if (middle == 3 && (last == 2))
                        return true;
                }
            }

            return false;
        }

        //public static bool IsProbablyTurkishPlate(string plateText)
        //{
        //    if (string.IsNullOrEmpty(plateText))
        //        return false;

        //    plateText = plateText.ToUpperInvariant();

        //    if (plateText.Length < 6 || plateText.Length > 9)
        //        return false; // Türk plakaları genellikle 6-9 karakter arasında olur

        //    int index = 0;

        //    // 1️⃣ Şehir kodu: İlk 2 karakter rakam olmalı
        //    if (index + 1 >= plateText.Length || !char.IsDigit(plateText[index]) || !char.IsDigit(plateText[index + 1]))

        //        return false;

        //    int cityCode = int.Parse(plateText.Substring(0, 2));
        //    if (cityCode < 1 || cityCode > 81)
        //        return false;

        //    index += 2;

        //    // 2️⃣ Plaka harfleri: 1-3 harf
        //    int letterCount = 0;
        //    while (index < plateText.Length && char.IsLetter(plateText[index]))
        //    {
        //        letterCount++;
        //        index++;
        //    }

        //    if (letterCount < 1 || letterCount > 3)
        //        return false;

        //    // 3️⃣ Plaka sonu: 2-4 rakam
        //    int numberCount = 0;
        //    while (index < plateText.Length && char.IsDigit(plateText[index]))
        //    {
        //        numberCount++;
        //        index++;
        //    }

        //    if (numberCount < 2 || numberCount > 4)
        //        return false;

        //    // 4️⃣ Plaka sonuna ulaştık mı?
        //    if (index != plateText.Length)
        //        return false; // hâlâ harf veya rakam varsa yanlış demektir

        //    return true;
        //}

        public static bool IsProbablyTurkishPlate(string plateText)
        {
            if (string.IsNullOrEmpty(plateText))
                return false;

            plateText = plateText.ToUpperInvariant();

            // Geçerli Türk plaka uzunlukları 7–9 karakter arasında olmalı
            if (plateText.Length < 7 || plateText.Length > 9)
                return false;

            // Şehir kodu: İlk 2 karakter rakam ve 01-81 arası olmalı
            if (!char.IsDigit(plateText[0]) || !char.IsDigit(plateText[1]))
                return false;

            int cityCode = int.Parse(plateText.Substring(0, 2));
            if (cityCode < 1 || cityCode > 81)
                return false;

            int index = 2;

            // Harf grubu: 1-3 harf
            int letterStart = index;
            while (index < plateText.Length && char.IsLetter(plateText[index]))
                index++;
            int letterCount = index - letterStart;

            if (letterCount < 1 || letterCount > 3)
                return false;

            // Rakam grubu: kalan karakterler
            int numberStart = index;
            while (index < plateText.Length && char.IsDigit(plateText[index]))
                index++;
            int numberCount = index - numberStart;

            // Fazladan karakter varsa geçersiz
            if (index != plateText.Length)
                return false;

            // 🎯 Şimdi tam olarak belirtilen kombinasyonlara göre kontrol edelim
            return
                (letterCount == 1 && (numberCount == 4 || numberCount == 5)) ||       // 99 X 9999, 99 X 99999
                (letterCount == 2 && (numberCount == 3 || numberCount == 4)) ||       // 99 XX 999, 99 XX 9999
                (letterCount == 3 && (numberCount == 2 || numberCount == 3));         // 99 XXX 99, 99 XXX 999
        }
    }
}
