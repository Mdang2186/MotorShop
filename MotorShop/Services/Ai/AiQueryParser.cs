using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MotorShop.ViewModels.Ai;

namespace MotorShop.Services.Ai
{
    public class AiQueryParser
    {
        // Regex bắt giá tiền (hỗ trợ "k", "nghìn", "tr", "triệu")
        private static readonly Regex PriceRegex =
            new(@"(dưới|tối đa|không quá|trên|tối thiểu|tầm|khoảng|~|<|>|giá dưới|giá trên)?\s*(\d{1,3}(?:[.,]\d)?)\s*(tr|triệu|t\b|k|nghìn)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex bắt chiều cao
        private static readonly Regex HeightRegex =
            new(@"(cao|height)?\s*(?:1m(?<h>\d{1,2})|(?<h>\d{2,3})\s*cm)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Danh sách tên xe để bắt dính (Context)
        // Danh sách tên xe để bắt dính (Context) - Đã mở rộng tối đa
        private static readonly Dictionary<string, string> SpecificModels = new()
        {
            // --- HONDA ---
            { "vision", "vision" }, { "vis", "vision" },
            { "airblade", "air blade" }, { "ab", "air blade" }, { "air blade", "air blade" },
            { "lead", "lead" }, { "ninja", "lead" }, // Vui vẻ: Ninjia lead
            { "sh", "sh" }, { "su hao", "sh" }, { "sh125", "sh" }, { "sh150", "sh" }, { "sh160", "sh" }, { "sh350", "sh" },
            { "mode", "sh mode" }, { "shmode", "sh mode" }, { "sh mode", "sh mode" },
            { "wave", "wave" }, { "alpha", "wave alpha" }, { "rsx", "wave rsx" }, { "blade", "blade" },
            { "future", "future" }, { "fu", "future" }, { "neo", "future" },
            { "winner", "winner" }, { "winnerx", "winner x" }, { "win", "winner" }, { "winx", "winner x" },
            { "vario", "vario" }, { "click", "click" }, { "sonic", "sonic" },
            { "pcx", "pcx" }, { "msx", "msx" }, { "cb", "cb" }, { "cbr", "cbr" }, { "cub", "cub" }, { "dream", "dream" },

            // --- YAMAHA ---
            { "exciter", "exciter" }, { "ex", "exciter" }, { "ex135", "exciter" }, { "ex150", "exciter" }, { "ex155", "exciter" },
            { "grande", "grande" }, { "grand", "grande" },
            { "janus", "janus" }, { "ja", "janus" },
            { "nvx", "nvx" }, { "aerox", "nvx" },
            { "sirius", "sirius" }, { "si", "sirius" },
            { "jupiter", "jupiter" }, { "ju", "jupiter" }, { "finn", "jupiter finn" },
            { "freego", "freego" }, { "latte", "latte" }, { "acruzo", "acruzo" },
            { "r15", "r15" }, { "r3", "r3" }, { "mt15", "mt-15" }, { "mt03", "mt-03" }, { "tfx", "tfx" },
            { "pg1", "pg-1" }, { "pg-1", "pg-1" },

            // --- SUZUKI ---
            { "raider", "raider" }, { "rai", "raider" },
            { "satria", "satria" }, { "sat", "satria" },
            { "gsx", "gsx" }, { "bandit", "gsx bandit" },
            { "burgman", "burgman" }, { "impulse", "impulse" }, { "address", "address" },

            // --- PIAGGIO / VESPA ---
            { "vespa", "vespa" },
            { "sprint", "vespa sprint" },
            { "primavera", "vespa primavera" }, { "pri", "vespa primavera" },
            { "gts", "vespa gts" },
            { "liberty", "liberty" }, { "lib", "liberty" },
            { "medley", "medley" },
            { "zip", "zip" },

            // --- VINFAST (XE ĐIỆN) ---
            { "vinfast", "vinfast" },
            { "klara", "klara" }, { "klara s", "klara" },
            { "feliz", "feliz" }, { "feliz s", "feliz" },
            { "vento", "vento" }, { "vento s", "vento" },
            { "evo", "evo" }, { "evo200", "evo" },
            { "theon", "theon" }, { "impes", "impes" }, { "ludo", "ludo" },

            // --- SYM ---
            { "attila", "attila" }, { "elizabeth", "attila" },
            { "shark", "shark" },
            { "galaxy", "galaxy" },
            { "elegant", "elegant" },
            { "angela", "angela" },

            // --- PKL / KHÁC ---
            { "z1000", "kawasaki z1000" }, { "ninja400", "kawasaki ninja" },
            { "monster", "ducati monster" }, { "panigale", "ducati panigale" },
            { "s1000rr", "bmw s1000rr" }
        };

        public AiParsedQuery Parse(string? text)
        {
            var q = new AiParsedQuery();
            if (string.IsNullOrWhiteSpace(text)) return q;

            text = text.Trim();
            var lower = text.ToLowerInvariant();

            // 1. Bắt tên xe (Quan trọng: để biết phụ tùng cho xe gì)
            foreach (var kvp in SpecificModels)
            {
                if (lower.Contains(kvp.Key))
                {
                    q.PreferredTags.Add($"model-{kvp.Value}");
                }
            }

            // 2. Bắt Loại Phụ Tùng (LOGIC MỚI THÊM)
            DetectParts(lower, q);

            // 3. Bắt Hãng xe (Brand)
            DetectBrands(lower, q);

            // 4. Bắt Ngân sách
            DetectBudget(lower, q);

            // 5. Các thông số khác (Chỉ dùng khi tìm xe máy)
            // Nếu tìm phụ tùng thì các thông số này ít quan trọng hơn, nhưng vẫn giữ để context đầy đủ
            DetectHeight(lower, q);
            DetectPurpose(lower, q);
            DetectExperience(lower, q);
            DetectFeatureTags(lower, q);

            return q;
        }

        // 1. Bảng từ khóa Phụ tùng (Mở rộng toàn diện)
        private static readonly Dictionary<string, string[]> PartKeywords = new()
        {
            // Nhóm Chất lỏng & Hóa chất
            { "part-oil",       new[] { "nhớt", "dầu máy", "dầu lap", "nước mát", "dung dịch", "súc động cơ", "vệ sinh buồng đốt", "bôi trơn", "xịt sên" } },
            
            // Nhóm Bánh xe
            { "part-tire",      new[] { "lốp", "vỏ xe", "bánh xe", "săm", "ruột xe", "vá vỏ", "van vòi" } },
            
            // Nhóm Phanh (Thắng)
            { "part-brake",     new[] { "phanh", "thắng", "bố thắng", "má phanh", "đĩa phanh", "heo dầu", "dây dầu", "tay thắng", "cùm thắng", "abs" } },
            
            // Nhóm Truyền động
            { "part-chain",     new[] { "sên", "xích", "nhông", "dĩa", "curoa", "dây đai", "bi nồi", "bố nồi", "chuông nồi", "lò xo nồi", "pulley" } },
            
            // Nhóm Lọc
            { "part-filter",    new[] { "lọc gió", "lọc nhớt", "lọc xăng", "pô air", "po air" } },
            
            // Nhóm Điện & Lửa
            { "part-battery",   new[] { "ắc quy", "bình điện", "bugi", "mobi", "mobin", "ic", "sạc", "đèn", "led", "xi nhan", "còi", "đèn pha", "trợ sáng" } },
            
            // Nhóm Kính & Gió
            { "part-mirror",    new[] { "gương", "kính chiếu hậu", "kính hậu", "chắn gió", "mão", "kính gió" } },
            
            // Nhóm Giảm xóc (Mới tách riêng cho rõ)
            { "part-suspension",new[] { "phuộc", "giảm xóc", "ty phuộc", "lò xo phuộc" } }, 
            
            // Nhóm Pô (Mới)
            { "part-exhaust",   new[] { "pô", "ống xả", "cổ pô", "tiêu pô" } }, 
            
            // Nhóm Phụ kiện & Đồ chơi & Dàn áo
            { "part-accessory", new[] { "bao tay", "gù", "baga", "cảng", "chống đổ", "ốc", "titan", "tem", "dàn áo", "yếm", "móc treo", "rổ", "gác chân", "chân chống" } }
        };

        // 2. Hàm xử lý logic (Gọn gàng hơn)
        private static void DetectParts(string lower, AiParsedQuery q)
        {
            bool isPartFound = false;

            // Duyệt qua từng nhóm từ khóa
            foreach (var group in PartKeywords)
            {
                // Nếu tìm thấy bất kỳ từ khóa nào trong nhóm
                if (group.Value.Any(keyword => lower.Contains(keyword)))
                {
                    q.PreferredTags.Add(group.Key);
                    isPartFound = true;
                }
            }

            // Kiểm tra các từ khóa tổng quát để xác định đây là nhu cầu tìm phụ tùng
            if (isPartFound ||
                lower.Contains("phụ tùng") ||
                lower.Contains("linh kiện") ||
                lower.Contains("đồ chơi") ||
                lower.Contains("phụ kiện") ||
                lower.Contains("đồ kiểng") ||
                q.PreferredTags.Any(t => t.StartsWith("part-")))
            {
                q.PreferredTags.Add("is-part-search");
            }
        }

        // --- CÁC HELPER CŨ (GIỮ NGUYÊN) ---
        // --- 3. Phát hiện Hãng (Brands) ---
        private static void DetectBrands(string lower, AiParsedQuery q)
        {
            // Danh sách hãng xe & phụ tùng phổ biến tại VN
            var brands = new[]
            { 
                // Xe máy
                "honda", "yamaha", "vinfast", "suzuki", "vespa", "piaggio", "sym", "ducati", "bmw", "kawasaki", "ktm", "triumph", "harley", "gpx", "benelli",
                // Phụ tùng / Dầu nhớt / Lốp
                "michelin", "dunlop", "maxxis", "irc", "kenda", "yokohama", "pirelli", // Lốp
                "motul", "castrol", "shell", "repsol", "liqui moly", "fuchs", "mobil", // Nhớt
                "ngk", "denso", "bosh", // Điện
                "did", "rk", "sss", // Nhông sên dĩa
                "brembo", "rcb", "racing boy", "nissin", "tokico", // Phanh
                "ohlins", "ysss", "nitron", "phuộc" // Phuộc
            };

            foreach (var b in brands)
            {
                // Kiểm tra từ khóa đứng độc lập hoặc trong cụm từ
                if (lower.Contains(b))
                {
                    q.PreferredBrands.Add(b);
                }
            }
        }

        // --- 4. Phát hiện Ngân sách (Budget) ---
        private static void DetectBudget(string lower, AiParsedQuery q)
        {
            // Regex đã được khai báo ở đầu class: PriceRegex
            var match = PriceRegex.Match(lower);
            if (match.Success)
            {
                string prefix = match.Groups[1].Value.ToLower(); // dưới, trên, tầm...
                string numStr = match.Groups[2].Value.Replace(',', '.'); // 2,5 -> 2.5
                string unit = match.Groups[3].Value.ToLower(); // tr, triệu, k...

                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal num))
                {
                    decimal multiplier = 1_000_000m; // Mặc định là triệu

                    // Xử lý đơn vị tiền tệ đa dạng
                    if (unit.Contains("k") || unit.Contains("nghìn") || unit.Contains("ngàn"))
                    {
                        multiplier = 1000m;
                    }
                    else if (unit.Contains("tr") || unit.Contains("triệu") || unit.Contains("củ"))
                    {
                        multiplier = 1_000_000m;
                    }
                    else if (unit.Contains("đ") || unit.Contains("vnd") || unit.Contains("đồng"))
                    {
                        // Nếu nhập số lớn (> 100.000) mà đơn vị là đ thì giữ nguyên
                        // Nếu nhập số nhỏ (< 1000) mà đơn vị là đ (ví dụ 500đ) thì có thể hiểu nhầm, 
                        // nhưng trong context mua xe/phụ tùng thường là k hoặc tr.
                        // Ở đây ta giả định nếu số nhỏ < 1000 mà đơn vị là đ/vnd thì đó là triệu (nói tắt) hoặc nghìn.
                        // Tuy nhiên để an toàn, ta check giá trị:
                        if (num < 1000) multiplier = 1_000_000m; // 500đ -> 500 triệu (xe PKL) hoặc 500k (phụ tùng) -> Khó phân biệt.
                        else multiplier = 1m; // 500.000đ -> 500.000
                    }

                    decimal money = num * multiplier;

                    // Xử lý ngữ cảnh (Prefix)
                    if (prefix.Contains("dưới") || prefix.Contains("tối đa") || prefix.Contains("không quá") || prefix.Contains("<") || prefix.Contains("rẻ hơn"))
                    {
                        q.BudgetMax = money;
                    }
                    else if (prefix.Contains("trên") || prefix.Contains("tối thiểu") || prefix.Contains("hơn") || prefix.Contains(">") || prefix.Contains("mắc hơn") || prefix.Contains("đắt hơn"))
                    {
                        q.BudgetMin = money;
                    }
                    else if (prefix.Contains("tầm") || prefix.Contains("khoảng") || prefix.Contains("quanh") || prefix.Contains("xấp xỉ") || prefix.Contains("~"))
                    {
                        // Biên độ 10%
                        q.BudgetMin = money * 0.9m;
                        q.BudgetMax = money * 1.1m;
                    }
                    else
                    {
                        // Trường hợp nói trống không "50 triệu" -> Coi như là ngân sách tối đa hoặc quanh đó
                        // Thường người dùng nói "có 50tr" nghĩa là max 50tr.
                        q.BudgetMax = money * 1.05m; // Cho dư 5%
                    }
                }
            }
        }

        // --- 5. Phát hiện Chiều cao (Height) ---
        private static void DetectHeight(string lower, AiParsedQuery q)
        {
            // Regex đã khai báo: HeightRegex
            var m = HeightRegex.Match(lower);
            if (m.Success)
            {
                // Trường hợp 1m65, 165cm
                if (int.TryParse(m.Groups["h"].Value, out var h))
                {
                    if (h < 100) h = h * 100 + (int.Parse(m.Groups[2].Value.PadRight(2, '0'))); // 1m6 -> 160 (Logic regex cũ cần chỉnh lại chút nếu muốn bắt chuẩn 1m6)
                                                                                                // Fix logic parse đơn giản hơn:
                                                                                                // Nếu chuỗi là "1m65" -> h=1, nhóm sau=65 -> 165
                                                                                                // Nếu chuỗi là "165cm" -> h=165

                    // Regex cũ: (?:1m(?<h>\d{1,2})|(?<h>\d{2,3})\s*cm)
                    // Nhóm <h> sẽ bắt được số mét lẻ hoặc số cm.

                    // Logic fix lại cho an toàn:
                    string val = m.Groups["h"].Value;
                    if (val.Length == 1 && lower.Contains("m")) // 1m...
                    {
                        // Tìm phần đuôi sau m
                        var part2 = Regex.Match(lower, @"1m(\d{1,2})");
                        if (part2.Success) h = 100 + int.Parse(part2.Groups[1].Value.PadRight(2, '0')); // 1m6 -> 160, 1m65 -> 165
                        else h = 100; // 1m
                    }
                    else
                    {
                        h = int.Parse(val);
                    }

                    if (h < 100) h += 100; // Fallback
                    if (h > 200) h = 175; // Chặn trên
                    q.HeightCm = h;
                }
            }
            else
            {
                // Từ khóa mô tả
                if (lower.Contains("thấp") || lower.Contains("lùn") || lower.Contains("nhỏ con") || lower.Contains("chân ngắn")) q.HeightCm = 155;
                else if (lower.Contains("cao") || lower.Contains("to cao") || lower.Contains("chân dài")) q.HeightCm = 175;
                else if (lower.Contains("trung bình") || lower.Contains("vừa")) q.HeightCm = 165;
            }
        }

        // --- 6. Phát hiện Mục đích sử dụng (Purpose) ---
        private static void DetectPurpose(string lower, AiParsedQuery q)
        {
            // Đi phố, đi làm
            if (lower.Contains("đi làm") || lower.Contains("đi học") || lower.Contains("phố") || lower.Contains("đô thị") || lower.Contains("văn phòng") || lower.Contains("hằng ngày"))
                q.Purpose = "city";

            // Chở hàng, chạy dịch vụ
            if (lower.Contains("shipper") || lower.Contains("ship") || lower.Contains("grab") || lower.Contains("be") || lower.Contains("gojek") || lower.Contains("chở hàng") || lower.Contains("thồ") || lower.Contains("cày"))
                q.Purpose = "delivery";

            // Đi chơi, đi xa
            if (lower.Contains("phượt") || lower.Contains("tour") || lower.Contains("đi xa") || lower.Contains("đường trường") || lower.Contains("du lịch") || lower.Contains("về quê"))
                q.Purpose = "touring";

            // Tốc độ, thể thao
            if (lower.Contains("đua") || lower.Contains("tốc độ") || lower.Contains("bốc") || lower.Contains("mạnh") || lower.Contains("sport"))
                q.Purpose = "sport";
        }

        // --- 7. Phát hiện Kinh nghiệm (Experience) ---
        private static void DetectExperience(string lower, AiParsedQuery q)
        {
            if (lower.Contains("mới") || lower.Contains("chưa quen") || lower.Contains("tập lái") || lower.Contains("lần đầu") || lower.Contains("nhập môn"))
                q.IsBeginner = true;

            if (lower.Contains("lâu năm") || lower.Contains("cứng") || lower.Contains("rành") || lower.Contains("quen") || lower.Contains("nâng cấp") || lower.Contains("lên đời"))
                q.IsBeginner = false;
        }

        // --- 8. Phát hiện Tính năng (Feature Tags) ---
        private static void DetectFeatureTags(string lower, AiParsedQuery q)
        {
            void Add(string s) { if (!q.PreferredTags.Contains(s)) q.PreferredTags.Add(s); }

            // Tiết kiệm
            if (lower.Contains("tiết kiệm") || lower.Contains("lợi xăng") || lower.Contains("ít hao") || lower.Contains("kinh tế"))
                Add("feature-fuel-saving");

            // Nhẹ, dễ dắt
            if (lower.Contains("nhẹ") || lower.Contains("gọn") || lower.Contains("dễ dắt") || lower.Contains("nữ") || lower.Contains("linh hoạt"))
            { Add("feature-lightweight"); Add("feature-low-seat"); }

            // Êm ái
            if (lower.Contains("êm") || lower.Contains("thoải mái") || lower.Contains("không rung") || lower.Contains("mượt"))
                Add("feature-comfort");

            // Thể thao
            if (lower.Contains("thể thao") || lower.Contains("mạnh") || lower.Contains("ngầu") || lower.Contains("bốc") || lower.Contains("cá tính"))
                Add("feature-sporty");

            // Sang trọng
            if (lower.Contains("sang") || lower.Contains("đẹp") || lower.Contains("thời trang") || lower.Contains("cao cấp") || lower.Contains("xịn"))
                Add("feature-premium");

            // Tiện ích
            if (lower.Contains("cốp rộng") || lower.Contains("đựng đồ") || lower.Contains("cốp to"))
                Add("feature-storage");
            if (lower.Contains("khóa thông minh") || lower.Contains("smartkey") || lower.Contains("smart key"))
                Add("feature-smartkey");
            if (lower.Contains("abs") || lower.Contains("chống bó cứng"))
                Add("feature-abs");

            // Logic thêm từ Height/Purpose
            if (q.HeightCm.HasValue)
            {
                if (q.HeightCm <= 160) Add("height-short");
                else if (q.HeightCm >= 175) Add("height-tall");
            }
            if (q.Purpose == "city") Add("usage-city");
            if (q.Purpose == "touring") Add("usage-touring");
            if (q.Purpose == "delivery") { Add("usage-delivery"); Add("feature-cheap-maintenance"); }
        }
    }
}