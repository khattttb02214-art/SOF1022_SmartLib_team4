using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SmartLib.Web.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NhoHonHoacBangAttribute : ValidationAttribute, IClientModelValidator
{
    private readonly string _tenThuocTinhKia;

    public NhoHonHoacBangAttribute(string tenThuocTinhKia)
    {
        _tenThuocTinhKia = tenThuocTinhKia;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var propKia = validationContext.ObjectType.GetProperty(_tenThuocTinhKia);
        if (propKia == null)
            return new ValidationResult($"Không tìm thấy thuộc tính '{_tenThuocTinhKia}' để so sánh.");

        var giaTriKia = propKia.GetValue(validationContext.ObjectInstance);

        // Thiếu 1 trong 2 giá trị thì bỏ qua — đã có [Required]/[Range] riêng lo phần
        // bắt buộc nhập, attribute này CHỈ lo mỗi phần so sánh lớn/nhỏ.
        if (value == null || giaTriKia == null) return ValidationResult.Success;

        long giaTriHienTai = Convert.ToInt64(value);
        long giaTriDoiChieu = Convert.ToInt64(giaTriKia);

        if (giaTriHienTai <= giaTriDoiChieu) return ValidationResult.Success;

        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        // Gán bằng indexer (không dùng .Add) để không văng lỗi trùng key nếu thuộc
        // tính còn gắn kèm attribute validate khác (VD: [Range]) cũng set "data-val".
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-nhohonhoacbang"] = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        context.Attributes["data-val-nhohonhoacbang-other"] = _tenThuocTinhKia;
    }
}
