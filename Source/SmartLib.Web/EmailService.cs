using System.Net;
using System.Net.Mail;

namespace SmartLib.Web.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = "";
    public string SenderName { get; set; } = "SmartLib";
    public string SenderPassword { get; set; } = "";
}

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _settings = config.GetSection("EmailSettings").Get<EmailSettings>() ?? new();
        _logger = logger;
    }

    public async Task SendOtpAsync(string toEmail, string toName, string otp, string purpose)
    {
        string subject, body;

        if (purpose == "register")
        {
            subject = "🔐 Xác minh đăng ký tài khoản SmartLib";
            body = BuildRegisterEmail(toName, otp);
        }
        else if (purpose == "change_email")
        {
            subject = "📧 Xác nhận đổi địa chỉ email SmartLib";
            body = BuildChangeEmailBody(toName, otp);
        }
        else
        {
            subject = "🔐 Mã OTP xác nhận thay đổi thông tin SmartLib";
            body = BuildEditProfileEmail(toName, otp);
        }

        await SendAsync(toEmail, toName, subject, body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                EnableSsl = true
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(msg);
            _logger.LogInformation("Email OTP đã gửi tới {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi email tới {Email}", toEmail);
            throw;
        }
    }

    // ── TEMPLATE: Đăng ký tài khoản ──────────────────────────────────────
    private static string BuildRegisterEmail(string name, string otp) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f4f3ff;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f3ff;padding:40px 16px;">
            <tr><td align="center">
              <table width="500" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:24px;overflow:hidden;box-shadow:0 8px 40px rgba(79,70,229,.15);max-width:100%;">

                <!-- Header -->
                <tr>
                  <td style="background:linear-gradient(135deg,#312e81 0%,#4338ca 50%,#6d28d9 100%);padding:36px 32px;text-align:center;">
                    <div style="width:64px;height:64px;background:rgba(255,255,255,.15);border-radius:18px;display:inline-flex;align-items:center;justify-content:center;margin-bottom:16px;">
                      <span style="font-size:30px;">📚</span>
                    </div>
                    <h1 style="color:#fff;font-size:26px;font-weight:800;margin:0 0 6px;">SmartLib</h1>
                    <p style="color:rgba(196,181,253,.85);font-size:14px;margin:0;">Thư Viện Thông Minh</p>
                  </td>
                </tr>

                <!-- Body -->
                <tr>
                  <td style="padding:40px 36px;">
                    <h2 style="color:#1e1b4b;font-size:20px;font-weight:800;margin:0 0 12px;">Xác minh đăng ký tài khoản</h2>
                    <p style="color:#4b5563;font-size:15px;line-height:1.6;margin:0 0 24px;">
                      Xin chào <strong style="color:#4f46e5;">{name}</strong>! 👋<br>
                      Cảm ơn bạn đã đăng ký tài khoản sinh viên trên <strong>SmartLib</strong>.
                      Vui lòng sử dụng mã xác minh dưới đây để hoàn tất đăng ký:
                    </p>

                    <!-- OTP Box -->
                    <div style="background:linear-gradient(135deg,#ede9fe 0%,#e0e7ff 100%);border:2px solid #c4b5fd;border-radius:16px;padding:28px;text-align:center;margin-bottom:24px;">
                      <p style="color:#4338ca;font-size:12px;font-weight:700;letter-spacing:2px;margin:0 0 12px;text-transform:uppercase;">Mã xác minh</p>
                      <div style="font-size:42px;font-weight:900;font-family:'Courier New',monospace;color:#4f46e5;letter-spacing:10px;line-height:1;">{otp}</div>
                      <p style="color:#7c3aed;font-size:12px;margin:12px 0 0;">⏱ Mã có hiệu lực trong <strong>10 phút</strong></p>
                    </div>

                    <div style="background:#fef9c3;border:1.5px solid #fde047;border-radius:12px;padding:14px 16px;margin-bottom:24px;">
                      <p style="color:#713f12;font-size:13px;margin:0;">⚠️ <strong>Lưu ý bảo mật:</strong> Không chia sẻ mã này với bất kỳ ai. SmartLib sẽ không bao giờ yêu cầu mã OTP qua điện thoại.</p>
                    </div>

                    <p style="color:#6b7280;font-size:13px;margin:0;">Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email.</p>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="background:#f9f8ff;border-top:1.5px solid #e0e7ff;padding:20px 36px;text-align:center;">
                    <p style="color:#9ca3af;font-size:12px;margin:0;">© 2026 SmartLib · Hệ thống thư viện thông minh</p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    // ── TEMPLATE: Sửa thông tin cá nhân ─────────────────────────────────
    private static string BuildEditProfileEmail(string name, string otp) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="UTF-8"></head>
        <body style="margin:0;padding:0;background:#f4f3ff;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f3ff;padding:40px 16px;">
            <tr><td align="center">
              <table width="500" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:24px;overflow:hidden;box-shadow:0 8px 40px rgba(79,70,229,.15);max-width:100%;">
                <tr>
                  <td style="background:linear-gradient(135deg,#312e81 0%,#4338ca 50%,#6d28d9 100%);padding:36px 32px;text-align:center;">
                    <div style="font-size:36px;margin-bottom:12px;">🛡️</div>
                    <h1 style="color:#fff;font-size:22px;font-weight:800;margin:0 0 4px;">Xác nhận thay đổi thông tin</h1>
                    <p style="color:rgba(196,181,253,.85);font-size:13px;margin:0;">SmartLib · Bảo mật tài khoản</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px 36px;">
                    <p style="color:#4b5563;font-size:15px;line-height:1.6;margin:0 0 24px;">
                      Xin chào <strong style="color:#4f46e5;">{name}</strong>,<br>
                      Chúng tôi nhận được yêu cầu <strong>thay đổi thông tin cá nhân</strong> trên tài khoản SmartLib của bạn.
                      Nhập mã OTP dưới đây để xác nhận:
                    </p>
                    <div style="background:linear-gradient(135deg,#ede9fe 0%,#e0e7ff 100%);border:2px solid #c4b5fd;border-radius:16px;padding:28px;text-align:center;margin-bottom:24px;">
                      <p style="color:#4338ca;font-size:12px;font-weight:700;letter-spacing:2px;margin:0 0 12px;text-transform:uppercase;">Mã OTP</p>
                      <div style="font-size:42px;font-weight:900;font-family:'Courier New',monospace;color:#4f46e5;letter-spacing:10px;line-height:1;">{otp}</div>
                      <p style="color:#7c3aed;font-size:12px;margin:12px 0 0;">⏱ Hiệu lực trong <strong>10 phút</strong></p>
                    </div>
                    <div style="background:#fee2e2;border:1.5px solid #fca5a5;border-radius:12px;padding:14px 16px;">
                      <p style="color:#991b1b;font-size:13px;margin:0;">🚨 Nếu bạn không thực hiện thao tác này, tài khoản của bạn có thể đang bị truy cập trái phép. Hãy liên hệ thủ thư ngay!</p>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td style="background:#f9f8ff;border-top:1.5px solid #e0e7ff;padding:20px 36px;text-align:center;">
                    <p style="color:#9ca3af;font-size:12px;margin:0;">© 2026 SmartLib · Hệ thống thư viện thông minh</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    // ── TEMPLATE: Đổi email ──────────────────────────────────────────────
    private static string BuildChangeEmailBody(string name, string otp) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="UTF-8"></head>
        <body style="margin:0;padding:0;background:#f4f3ff;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f3ff;padding:40px 16px;">
            <tr><td align="center">
              <table width="500" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:24px;overflow:hidden;box-shadow:0 8px 40px rgba(79,70,229,.15);max-width:100%;">
                <tr>
                  <td style="background:linear-gradient(135deg,#0f172a 0%,#1e3a5f 50%,#1d4ed8 100%);padding:36px 32px;text-align:center;">
                    <div style="font-size:36px;margin-bottom:12px;">📧</div>
                    <h1 style="color:#fff;font-size:22px;font-weight:800;margin:0 0 4px;">Xác nhận địa chỉ email mới</h1>
                    <p style="color:rgba(147,197,253,.85);font-size:13px;margin:0;">SmartLib · Xác minh email</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px 36px;">
                    <p style="color:#4b5563;font-size:15px;line-height:1.6;margin:0 0 24px;">
                      Xin chào <strong style="color:#1d4ed8;">{name}</strong>,<br>
                      Bạn vừa yêu cầu <strong>thay đổi địa chỉ email</strong> trên SmartLib. Nhập mã OTP dưới đây để xác nhận email mới:
                    </p>
                    <div style="background:linear-gradient(135deg,#dbeafe 0%,#e0f2fe 100%);border:2px solid #93c5fd;border-radius:16px;padding:28px;text-align:center;margin-bottom:24px;">
                      <p style="color:#1d4ed8;font-size:12px;font-weight:700;letter-spacing:2px;margin:0 0 12px;text-transform:uppercase;">Mã xác nhận email</p>
                      <div style="font-size:42px;font-weight:900;font-family:'Courier New',monospace;color:#1e40af;letter-spacing:10px;line-height:1;">{otp}</div>
                      <p style="color:#2563eb;font-size:12px;margin:12px 0 0;">⏱ Hiệu lực trong <strong>10 phút</strong></p>
                    </div>
                    <div style="background:#fef9c3;border:1.5px solid #fde047;border-radius:12px;padding:14px 16px;">
                      <p style="color:#713f12;font-size:13px;margin:0;">⚠️ Email cũ của bạn vẫn hoạt động cho đến khi xác nhận hoàn tất.</p>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td style="background:#f9f8ff;border-top:1.5px solid #e0e7ff;padding:20px 36px;text-align:center;">
                    <p style="color:#9ca3af;font-size:12px;margin:0;">© 2026 SmartLib · Hệ thống thư viện thông minh</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
