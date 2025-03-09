using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GorevZamanlayici
{
    // Görev için basit bir model
    class Gorev
    {
        public string Ad { get; set; }
        public DateTime CalismaZamani { get; set; }
        public bool TekrarEtsinMi { get; set; }
        public TimeSpan? TekrarAraligi { get; set; }
    }

    // Zamanlayıcıyı yöneten ana sınıf
    class Zamanlayici
    {
        private List<Gorev> gorevler = new List<Gorev>();
        private readonly string dosyaYolu = "gorevler.json";
        private bool calisiyorMu = false;

        // Görevleri JSON'dan çekelim
        public async Task GorevleriYukle()
        {
            if (File.Exists(dosyaYolu))
            {
                var json = await File.ReadAllTextAsync(dosyaYolu);
                gorevler = JsonSerializer.Deserialize<List<Gorev>>(json) ?? new List<Gorev>();
            }
        }

        // Görevleri JSON'a kaydet
        private async Task GorevleriKaydet()
        {
            var json = JsonSerializer.Serialize(gorevler, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dosyaYolu, json);
        }

        // Yeni görev ekleme
        public void GorevEkle(string ad, DateTime zaman, bool tekrarEtsinMi = false, TimeSpan? aralik = null)
        {
            gorevler.Add(new Gorev
            {
                Ad = ad,
                CalismaZamani = zaman,
                TekrarEtsinMi = tekrarEtsinMi,
                TekrarAraligi = aralik
            });
        }

        // Zamanlayıcıyı çalıştır
        public async Task Baslat()
        {
            calisiyorMu = true;
            await GorevleriYukle();

            while (calisiyorMu)
            {
                var simdikiZaman = DateTime.Now;
                var hazirGorevler = new List<Gorev>();
                
                // Zamanı gelmiş görevleri bul
                foreach (var gorev in gorevler)
                {
                    if (gorev.CalismaZamani <= simdikiZaman)
                        hazirGorevler.Add(gorev);
                }

                // Hazır görevleri çalıştır
                foreach (var gorev in hazirGorevler)
                {
                    Console.WriteLine($"{gorev.Ad} şu anda çalışıyor - {simdikiZaman}");
                    await GoreviCalistir(gorev);

                    if (gorev.TekrarEtsinMi && gorev.TekrarAraligi.HasValue)
                    {
                        gorev.CalismaZamani = simdikiZaman + gorev.TekrarAraligi.Value;
                    }
                    else
                    {
                        gorevler.Remove(gorev);
                    }
                }

                await GorevleriKaydet();
                await Task.Delay(1000); // 1 saniye nefes alalım
            }
        }

        // Zamanlayıcıyı durdur
        public void Durdur()
        {
            calisiyorMu = false;
        }

        // Görevi çalıştıran metod (burayı özelleştirebilirsin)
        private async Task GoreviCalistir(Gorev gorev)
        {
            await Task.Run(() => Console.WriteLine($"{gorev.Ad} tamamlandı!"));
        }

        // Görevleri ekrana yazdır
        public void GorevleriListele()
        {
            if (gorevler.Count == 0)
            {
                Console.WriteLine("Hiç görev yok, neyi bekliyorsun?");
                return;
            }

            foreach (var gorev in gorevler)
            {
                var tekrarDurumu = gorev.TekrarEtsinMi ? " (Tekrarlı)" : "";
                Console.WriteLine($"Görev: {gorev.Ad}, Zaman: {gorev.CalismaZamani}{tekrarDurumu}");
            }
        }
    }

    // Ana program
    class Program
    {
        static async Task Main(string[] args)
        {
            var zamanlayici = new Zamanlayici();

            // Birkaç örnek görev ekleyelim
            zamanlayici.GorevEkle("Sunucuyu kontrol et", DateTime.Now.AddSeconds(3));
            zamanlayici.GorevEkle("Raporu mail at", DateTime.Now.AddSeconds(7), true, TimeSpan.FromSeconds(15));

            // Görevleri gösterelim
            Console.WriteLine("Şu anki görevler:");
            zamanlayici.GorevleriListele();

            // Zamanlayıcıyı çalıştıralım
            Console.WriteLine("\nZamanlayıcı devrede, işler yolunda!");
            var zamanlayiciGorev = zamanlayici.Baslat();

            // Kullanıcı çıkmak isteyene kadar bekleyelim
            Console.WriteLine("Çıkmak için 'q' tuşuna bas.");
            while (Console.ReadKey().KeyChar != 'q') { }
            zamanlayici.Durdur();

            await zamanlayiciGorev;
            Console.WriteLine("\nZamanlayıcı kapandı, görüşürüz!");
        }
    }
}
