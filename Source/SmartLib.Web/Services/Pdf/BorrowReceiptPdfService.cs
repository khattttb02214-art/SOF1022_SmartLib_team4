using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartLib.Web.Models;

namespace SmartLib.Web.Services.Pdf;

public class BorrowReceiptPdfService
{
    public byte[] Generate(
    MuonTra borrow)
    {
        QuestPDF.Settings.License =
        LicenseType.Community;

    var document =
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("PHIẾU MƯỢN SÁCH")
                    .FontSize(24)
                    .Bold();

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text(
                            $"Mã phiếu: {borrow.MaPhieu}");

                        column.Item().Text(
                            $"Độc giả: {borrow.DocGia?.HoTen}");

                        column.Item().Text(
                            $"Ngày mượn: {borrow.NgayMuon:dd/MM/yyyy}");

                        column.Item().Text(
                            $"Ngày hẹn trả: {borrow.NgayHenTra:dd/MM/yyyy}");

                        column.Item()
                            .PaddingTop(20)
                            .Text("Danh sách sách:")
                            .Bold();

                        foreach (var item in borrow.ChiTietMuonTras)
                        {
                            column.Item().Text(
                                $"- {item.Sach?.TenSach}");
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("SmartLib Library System");
                    });
            });
        });

        return document.GeneratePdf();
    }


}
