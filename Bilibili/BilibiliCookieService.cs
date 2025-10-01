/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library.bilibili
 * 唯一标识：3f99b2a4-79bf-4c52-9c15-883ba68ca488
 * 文件名：BilibiliCookieService
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/30 0:18:55
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：
 * 时间：
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/


using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace HaiTang.library.bilibili
{
    /// <summary>
    /// B站Cookie服务类
    /// 处理扫码登录、Cookie保存和验证
    /// </summary>
    public class BilibiliCookieService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _cookieFilePath;
        private CancellationTokenSource _pollingCancellationTokenSource;

        public BilibiliCookieService(string cookieFilePath = "cookies.dat")
        {
            _cookieFilePath = cookieFilePath;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<(string qrcodeKey, string qrcodeUrl)> GenerateQRCodeAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://passport.bilibili.com/x/passport-login/web/qrcode/generate");
                using var doc = JsonDocument.Parse(response);
                var data = doc.RootElement.GetProperty("data");

                string qrcodeUrl = data.GetProperty("url").GetString() ?? string.Empty;
                string qrcodeKey = data.GetProperty("qrcode_key").GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(qrcodeUrl) || string.IsNullOrEmpty(qrcodeKey))
                {
                    throw new Exception("获取二维码信息失败");
                }

                return (qrcodeKey, qrcodeUrl);
            }
            catch (Exception ex)
            {
                throw new Exception($"生成二维码失败: {ex.Message}");
            }
        }

        public async Task<LoginResult> StartPollingLoginStatusAsync(string qrcodeKey, IProgress<string> progress = null)
        {
            _pollingCancellationTokenSource = new CancellationTokenSource();
            var result = new LoginResult();

            try
            {
                int maxRetry = 60;
                int currentRetry = 0;

                while (currentRetry < maxRetry && !_pollingCancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(
                            $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}",
                            _pollingCancellationTokenSource.Token);

                        var responseText = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseText);
                        var data = doc.RootElement.GetProperty("data");
                        int code = data.GetProperty("code").GetInt32();
                        string message = data.GetProperty("message").GetString() ?? string.Empty;

                        switch (code)
                        {
                            case 0:
                                result.Success = true;
                                result.Message = "登录成功！";

                                if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
                                {
                                    ExtractCookiesFromHeaders(cookieHeaders, result);
                                }

                                if (data.TryGetProperty("refresh_token", out var refreshToken))
                                {
                                    result.RefreshToken = refreshToken.GetString() ?? string.Empty;
                                }

                                progress?.Report("登录成功！");
                                return result;

                            case 86038:
                                result.Success = false;
                                result.Message = "二维码已过期，请重新生成";
                                progress?.Report("二维码已过期");
                                return result;

                            case 86090:
                                progress?.Report("二维码已扫描，请在手机上确认");
                                break;

                            case 86101:
                                progress?.Report("等待扫码...");
                                break;

                            default:
                                progress?.Report($"状态: {message}");
                                break;
                        }

                        await Task.Delay(3000, _pollingCancellationTokenSource.Token);
                        currentRetry++;
                    }
                    catch (TaskCanceledException)
                    {
                        result.Success = false;
                        result.Message = "登录已取消";
                        break;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"轮询错误: {ex.Message}");
                        await Task.Delay(3000, _pollingCancellationTokenSource.Token);
                        currentRetry++;
                    }
                }

                if (!result.Success && string.IsNullOrEmpty(result.Message))
                {
                    result.Success = false;
                    result.Message = "登录超时";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"登录过程出错: {ex.Message}";
                return result;
            }
        }

        public void StopPolling()
        {
            _pollingCancellationTokenSource?.Cancel();
        }

        private void ExtractCookiesFromHeaders(IEnumerable<string> cookieHeaders, LoginResult result)
        {
            foreach (var header in cookieHeaders)
            {
                var cookies = header.Split(';');
                foreach (var cookie in cookies)
                {
                    var parts = cookie.Trim().Split('=');
                    if (parts.Length >= 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();

                        switch (name)
                        {
                            case "SESSDATA":
                                result.SessData = value;
                                break;
                            case "bili_jct":
                                result.BiliJct = value;
                                break;
                            case "DedeUserID":
                                result.DedeUserID = value;
                                break;
                        }
                    }
                }
            }
        }

        public void SaveCookies(LoginResult loginResult)
        {
            try
            {
                var cookieData = new CookieData
                {
                    SessData = loginResult.SessData,
                    BiliJct = loginResult.BiliJct,
                    DedeUserID = loginResult.DedeUserID,
                    RefreshToken = loginResult.RefreshToken,
                    LastUpdate = DateTime.Now,
                    ExpireTime = DateTime.Now.AddMonths(1)
                };

                string json = JsonSerializer.Serialize(cookieData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                byte[] encryptedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(json),
                    null,
                    DataProtectionScope.LocalMachine);

                File.WriteAllBytes(_cookieFilePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存Cookie失败: {ex.Message}");
            }
        }

        public CookieData LoadCookies()
        {
            try
            {
                if (!File.Exists(_cookieFilePath))
                {
                    return null;
                }

                byte[] encryptedBytes = File.ReadAllBytes(_cookieFilePath);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
                string json = Encoding.UTF8.GetString(decryptedBytes);

                return JsonSerializer.Deserialize<CookieData>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"读取Cookie失败: {ex.Message}");
            }
        }

        public async Task<bool> ValidateCookiesAsync(CookieData cookies)
        {
            if (cookies == null || string.IsNullOrEmpty(cookies.SessData) || cookies.ExpireTime < DateTime.Now)
                return false;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={cookies.SessData}; bili_jct={cookies.BiliJct}");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await client.GetAsync("https://api.bilibili.com/x/space/myinfo");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void ClearCookies()
        {
            if (File.Exists(_cookieFilePath))
            {
                File.Delete(_cookieFilePath);
            }
        }

        public void Dispose()
        {
            _pollingCancellationTokenSource?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
