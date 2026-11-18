SET NOCOUNT ON;

DECLARE @Seed TABLE (
    Slug nvarchar(450), ProductId int, Title nvarchar(250), Model nvarchar(120),
    Summary nvarchar(500), Html nvarchar(max), LegacyHtml nvarchar(max), LegacySummary nvarchar(500), PlainText nvarchar(max)
);

WITH Guides AS
(
    SELECT * FROM (VALUES
      (N'5008s', N'fresenious-medicalcare-fmc', N'Máy lọc thận Online HDF 5008S', N'5008S', N'5008S là model máy lọc thận Online HDF được Anpha liệt kê trong danh mục Fresenius Medical Care. Khi tìm tài liệu, hãy kiểm tra mã máy và phiên bản phần mềm trên hồ sơ thiết bị.'),
      (N'4008', N'fresenious-medicalcare-fmc', N'Máy thận nhân tạo 4008', N'4008', N'Anpha liệt kê 4008 trong nhóm máy thận nhân tạo của Fresenius Medical Care. Tên dòng 4008 có thể xuất hiện trên nhiều cấu hình, nên cần đối chiếu model đầy đủ trước khi yêu cầu tài liệu.'),
      (N'multifiltrate', N'fresenious-medicalcare-fmc', N'Máy lọc máu liên tục MultiFiltrate', N'MultiFiltrate', N'MultiFiltrate nằm trong nhóm máy lọc máu liên tục (CRRT) của Fresenius Medical Care. Ghi lại tên đầy đủ trên nhãn để phân biệt các phiên bản mang tên gần giống nhau.'),
      (N'bibag', N'fresenious-medicalcare-fmc', N'Bột dịch lọc thận bibag', N'bibag', N'bibag được Anpha giới thiệu là bột dịch lọc thận của Fresenius Medical Care. Khi yêu cầu tài liệu, gửi kèm mã REF và quy cách trên nhãn bao bì.'),
      (N'cre8', N'c-i-d', N'Stent mạch vành Cre8', N'Cre8', N'Cre8 được liệt kê trong nhóm stent mạch vành của CID / Alvimedica. Chọn tài liệu theo mã REF ghi trên bao bì vì tên thương mại có thể dùng cho nhiều mã.'),
      (N'avantgarde', N'c-i-d', N'Stent mạch vành Avantgarde', N'Avantgarde', N'Avantgarde được Anpha giới thiệu trong nhóm stent mạch vành của CID / Alvimedica. Dùng mã REF và tên đầy đủ trên nhãn để xác định hồ sơ tương ứng.'),
      (N'fluydo-ptca', N'c-i-d', N'Ống thông nong bóng Fluydo PTCA', N'Fluydo PTCA', N'Fluydo PTCA là dòng ống thông nong bóng trong danh mục CID / Alvimedica. Khi tra cứu tài liệu, đối chiếu tên dòng và mã REF trên bao bì.'),
      (N'open-pivot', N'medtronic', N'Van tim cơ học Open Pivot Mitral', N'Open Pivot', N'Open Pivot Mitral là tên dòng van tim cơ học được liệt kê trong danh mục Medtronic của Anpha. Kiểm tra tên biến thể và mã REF khi yêu cầu hồ sơ.'),
      (N'hancock-ii', N'medtronic', N'Van tim sinh học Hancock II', N'Hancock II', N'Hancock II được Anpha liệt kê trong nhóm van tim sinh học Medtronic. Để tìm đúng tài liệu, đối chiếu tên model đầy đủ và mã REF trên nhãn.'),
      (N'cg-future', N'medtronic', N'Vòng van CG Future', N'CG Future', N'CG Future thuộc nhóm vòng van của Medtronic trong danh mục Anpha. Dùng tên cấu hình và mã REF để phân biệt các bản Ring hoặc Band được liệt kê.'),
      (N'profile-3d', N'medtronic', N'Vòng van Profile 3D', N'Profile 3D', N'Profile 3D là một dòng vòng van Medtronic trong danh mục Anpha. Giữ nguyên tên Profile 3D và mã REF khi yêu cầu hướng dẫn hoặc tài liệu kỹ thuật.'),
      (N'melody', N'medtronic', N'Van động mạch phổi Melody', N'Melody', N'Melody được Anpha liệt kê trong nhóm van động mạch phổi Medtronic. Đối chiếu mã REF của sản phẩm khi tìm tài liệu, nhất là khi hồ sơ có cả bộ đưa van.'),
      (N'corevalve', N'medtronic', N'Van động mạch chủ CoreValve', N'CoreValve', N'CoreValve được Anpha liệt kê trong nhóm van động mạch chủ Medtronic. Gửi tên hệ thống và mã REF đầy đủ để đối chiếu tài liệu phù hợp.'),
      (N'nagor', N'nagor-page', N'Danh mục túi cấy ghép Nagor', N'Nagor', N'Danh mục Nagor trên Anpha gồm IMPLEO, CoGel, RGI và GFX. Hãy dùng đúng tên dòng và mã REF để tìm hồ sơ cụ thể thay vì chỉ tìm theo thương hiệu.'),
      (N'impleo', N'tui-nang-nguc-impleo', N'Dòng túi cấy ghép IMPLEO', N'IMPLEO', N'IMPLEO là dòng túi cấy ghép của Nagor trong danh mục Anpha. Trang sản phẩm có mô tả dòng và thông tin bảo hành. Khi yêu cầu hướng dẫn, đối chiếu kiểu dáng và mã REF ghi trên nhãn hoặc thẻ sản phẩm.'),
      (N'cogel', N'cogel', N'Dòng túi cấy ghép CoGel', N'CoGel', N'CoGel là dòng túi cấy ghép Nagor được Anpha giới thiệu với các lựa chọn hình dáng và độ nhô. Đối chiếu tên biến thể, kích thước và mã REF của sản phẩm khi tra cứu.'),
      (N'rgi', N'rgi', N'Dòng túi cấy ghép RGI', N'RGI', N'RGI được liệt kê là một dòng riêng trong danh mục cấy ghép Nagor. Gửi mã REF và thông tin nhận diện trên nhãn để yêu cầu đúng hồ sơ sản phẩm.'),
      (N'gfx', N'gfx', N'Dòng túi cấy ghép GFX', N'GFX', N'GFX là dòng túi cấy ghép Nagor trong danh mục Anpha. Khi trao đổi với nhà phân phối, dùng tên dòng GFX cùng mã REF để xác định tài liệu.')
    ) AS models(Slug, ProductSlug, Title, Model, Description)
)
INSERT @Seed (Slug, ProductId, Title, Model, Summary, Html, LegacyHtml, LegacySummary, PlainText)
SELECT N'hdsd-' + guide.Slug, product.Id, guide.Title, guide.Model,
    N'Tra cứu model ' + guide.Model + N' và tài liệu sản phẩm ' + product.Brand + N'.',
    N'<h2>Nhận diện sản phẩm</h2><p>' + guide.Description + N'</p>' +
    N'<p><a href="/san-pham/' + guide.ProductSlug + N'">Xem ảnh và thông tin trong danh mục Anpha</a></p>' +
    N'<h2>Yêu cầu đúng phiên bản tài liệu</h2><ul>' +
    N'<li>Tên dòng, model và mã REF trên nhãn sản phẩm.</li>' +
    N'<li>Phiên bản tài liệu và ngôn ngữ cần nhận.</li>' +
    N'</ul><p><a href="/lien-he?sanpham=' + guide.ProductSlug + N'">Gửi yêu cầu tài liệu cho Anpha</a></p>',
    N'<h2>Thông tin tài liệu</h2><p>Mẫu tra cứu cho <strong>' + guide.Title + N'</strong> (' + guide.Model +
    N').</p><dl><dt>Nhà sản xuất / thương hiệu</dt><dd>' + brand.SafeName +
    N'</dd><dt>Tệp hướng dẫn chính thức</dt><dd>Đang chờ bổ sung bản đã xác minh.</dd></dl>' +
    N'<h2>Đối chiếu khi bổ sung tài liệu</h2><p>Xác nhận mã REF trên nhãn, ngôn ngữ và phiên bản tài liệu với nhà cung cấp.</p>' +
    N'<p>Nội dung mẫu để duyệt giao diện. Không dùng tài liệu này để vận hành thiết bị hoặc hướng dẫn chăm sóc.</p>',
    N'Hồ sơ tài liệu mẫu cho ' + guide.Model + N'. Đang chờ hướng dẫn chính thức.',
    guide.Title + N' ' + guide.Model + N' ' + product.Brand + N' ' + guide.Description
FROM Guides AS guide
JOIN dbo.ContentPages AS parent ON parent.Slug = guide.ProductSlug
JOIN dbo.Products AS product ON product.ContentPageId = parent.Id
CROSS APPLY (VALUES (REPLACE(REPLACE(REPLACE(product.Brand, N'&', N'&amp;'), N'<', N'&lt;'), N'>', N'&gt;'))) AS brand(SafeName);

-- Upgrade the previous default only when editors have left that text untouched.
UPDATE page
SET Html = seed.Html, Summary = seed.Summary, PlainText = seed.PlainText, UpdatedAt = SYSUTCDATETIME()
FROM dbo.ContentPages AS page
JOIN @Seed AS seed ON seed.Slug = page.Slug AND seed.ProductId = page.GuideProductId
WHERE page.ContentKind = N'Guide' AND page.IsGuideSample = 1
    AND page.Html COLLATE Latin1_General_100_BIN2 = seed.LegacyHtml COLLATE Latin1_General_100_BIN2
    AND page.Summary COLLATE Latin1_General_100_BIN2 = seed.LegacySummary COLLATE Latin1_General_100_BIN2;

INSERT dbo.ContentPages (Slug, Title, ContentKind, Summary, Html, PlainText, IsPublished, PublishedAt,
    GuideProductId, GuideModel, IsGuideSample)
SELECT seed.Slug, seed.Title, N'Guide', seed.Summary, seed.Html, seed.PlainText, 1, SYSUTCDATETIME(),
    seed.ProductId, seed.Model, 1
FROM @Seed AS seed
WHERE NOT EXISTS (SELECT 1 FROM dbo.ContentPages WITH (UPDLOCK, HOLDLOCK) WHERE Slug = seed.Slug);
