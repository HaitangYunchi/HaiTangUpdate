/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library.bilibili
 * 唯一标识：33b10d28-6099-490a-b836-01d3b911b27d
 * 文件名：BilibiliUserService
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/30 0:18:00
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


using System.Text.Json;

namespace HaiTang.library.bilibili
{
    /// <summary>
    /// B站用户信息服务类 - 更健壮的版本
    /// </summary>
    public class BilibiliUserService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public BilibiliUserService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 配置更宽松的JSON序列化选项
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// API响应基础模型
        /// </summary>
        private class ApiResponse<T>
        {
            public int Code { get; set; }
            public string Message { get; set; } = string.Empty;
            public T Data { get; set; }
        }

        public void SetUserCookies(string sessData, string biliJct)
        {
            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            if (!string.IsNullOrEmpty(sessData))
            {
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessData}; bili_jct={biliJct}");
            }
        }

        /// <summary>
        /// 获取当前登录用户信息 - 使用更安全的方法
        /// </summary>
        public async Task<BlibiliUserInfo> GetCurrentUserInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.bilibili.com/x/space/myinfo");

                // 先尝试直接解析为JsonDocument来检查数据结构
                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                // 检查API响应代码
                if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 0)
                {
                    var message = root.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString()
                        : "未知错误";
                    throw new Exception($"API错误: {message} (Code: {codeElement.GetInt32()})");
                }

                // 检查数据是否存在
                if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind == JsonValueKind.Null)
                {
                    throw new Exception("API返回数据为空");
                }

                // 使用安全的反序列化方法
                try
                {
                    return JsonSerializer.Deserialize<BlibiliUserInfo>(dataElement.GetRawText(), _jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    // 如果标准反序列化失败，使用手动解析
                    return ParseUserInfoManually(dataElement);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 手动解析用户信息 - 处理字段类型不匹配的情况
        /// </summary>
        private BlibiliUserInfo ParseUserInfoManually(JsonElement dataElement)
        {
            var userInfo = new BlibiliUserInfo();

            try
            {
                // 手动解析每个字段，提供默认值
                if (dataElement.TryGetProperty("mid", out var midElement))
                {
                    userInfo.Mid = midElement.ValueKind switch
                    {
                        JsonValueKind.Number => midElement.GetInt64(),
                        JsonValueKind.String => long.TryParse(midElement.GetString(), out var mid) ? mid : 0,
                        _ => 0
                    };
                }

                if (dataElement.TryGetProperty("name", out var nameElement))
                {
                    userInfo.Name = nameElement.ValueKind == JsonValueKind.String ? nameElement.GetString() ?? string.Empty : string.Empty;
                }

                if (dataElement.TryGetProperty("sex", out var sexElement))
                {
                    userInfo.Sex = sexElement.ValueKind == JsonValueKind.String ? sexElement.GetString() ?? string.Empty : string.Empty;
                }

                if (dataElement.TryGetProperty("face", out var faceElement))
                {
                    userInfo.Face = faceElement.ValueKind == JsonValueKind.String ? faceElement.GetString() ?? string.Empty : string.Empty;
                }

                if (dataElement.TryGetProperty("sign", out var signElement))
                {
                    userInfo.Sign = signElement.ValueKind == JsonValueKind.String ? signElement.GetString() ?? string.Empty : string.Empty;
                }

                if (dataElement.TryGetProperty("level", out var levelElement))
                {
                    userInfo.Level = levelElement.ValueKind == JsonValueKind.Number ? levelElement.GetInt32() : 0;
                }

                // 解析生日字段 - 使用自定义逻辑
                if (dataElement.TryGetProperty("birthday", out var birthdayElement))
                {
                    userInfo.Birthday = birthdayElement.ValueKind switch
                    {
                        JsonValueKind.String => birthdayElement.GetString() ?? "未设置",
                        JsonValueKind.Number => birthdayElement.GetInt64() == 0 ? "未设置" : "已设置",
                        JsonValueKind.Null => "未设置",
                        _ => "未知"
                    };
                }

                // 解析硬币数量
                if (dataElement.TryGetProperty("coins", out var coinsElement))
                {
                    userInfo.Coins = coinsElement.ValueKind switch
                    {
                        JsonValueKind.Number => coinsElement.GetDecimal(),
                        JsonValueKind.String => decimal.TryParse(coinsElement.GetString(), out var coins) ? coins : 0,
                        _ => 0
                    };
                }

                // 解析VIP信息
                if (dataElement.TryGetProperty("vip", out var vipElement))
                {
                    userInfo.Vip = new VipInfo();

                    if (vipElement.TryGetProperty("type", out var typeElement))
                    {
                        userInfo.Vip.Type = typeElement.ValueKind == JsonValueKind.Number ? typeElement.GetInt32() : 0;
                    }

                    if (vipElement.TryGetProperty("status", out var statusElement))
                    {
                        userInfo.Vip.Status = statusElement.ValueKind == JsonValueKind.Number ? statusElement.GetInt32() : 0;
                    }
                }

                return userInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"手动解析用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过UID获取用户信息
        /// </summary>
        public async Task<BlibiliUserInfo> GetUserInfoByUidAsync(long uid)
        {
            try
            {
                // 使用WBI签名接口
                var response = await _httpClient.GetStringAsync($"https://api.bilibili.com/x/space/wbi/acc/info?mid={uid}");

                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 0)
                {
                    var message = root.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString()
                        : "未知错误";
                    throw new Exception($"API错误: {message} (Code: {codeElement.GetInt32()})");
                }

                if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind == JsonValueKind.Null)
                {
                    throw new Exception("API返回数据为空");
                }

                try
                {
                    return JsonSerializer.Deserialize<BlibiliUserInfo>(dataElement.GetRawText(), _jsonOptions);
                }
                catch (JsonException)
                {
                    return ParseUserInfoManually(dataElement);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取用户关系统计
        /// </summary>
        public async Task<UserStat> GetUserRelationStatAsync(long uid)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://api.bilibili.com/x/relation/stat?vmid={uid}");
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserStat>>(response, _jsonOptions);

                if (apiResponse?.Code == 0 && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                else
                {
                    throw new Exception($"API错误: {apiResponse?.Message ?? "未知错误"} (Code: {apiResponse?.Code})");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取用户关系统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取UP主统计信息
        /// </summary>
        public async Task<UpStat> GetUpStatAsync(long uid)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://api.bilibili.com/x/space/upstat?mid={uid}");
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UpStat>>(response, _jsonOptions);

                if (apiResponse?.Code == 0 && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                else
                {
                    throw new Exception($"API错误: {apiResponse?.Message ?? "未知错误"} (Code: {apiResponse?.Code})");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取UP主统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取完整的用户信息
        /// </summary>
        public async Task<Dictionary<string, object>> GetCompleteUserInfoAsync(long uid = 0)
        {
            var result = new Dictionary<string, object>();

            try
            {
                BlibiliUserInfo userInfo;
                if (uid == 0)
                {
                    userInfo = await GetCurrentUserInfoAsync();
                }
                else
                {
                    userInfo = await GetUserInfoByUidAsync(uid);
                }

                result["基础信息"] = userInfo;

                // 获取关系统计
                try
                {
                    var relationStat = await GetUserRelationStatAsync(userInfo.Mid);
                    result["关系统计"] = relationStat;
                }
                catch (Exception ex)
                {
                    result["关系统计错误"] = ex.Message;
                }

                // 获取UP主统计
                try
                {
                    var upStat = await GetUpStatAsync(userInfo.Mid);
                    result["UP主统计"] = upStat;
                }
                catch (Exception ex)
                {
                    result["UP主统计错误"] = ex.Message;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取完整用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取简化的用户信息（用于调试）
        /// </summary>
        public async Task<Dictionary<string, object>> GetSimpleUserInfoAsync()
        {
            try
            {
                var userInfo = await GetCurrentUserInfoAsync();
                return new Dictionary<string, object>
                {
                    ["UID"] = userInfo.Mid,
                    ["昵称"] = userInfo.Name,
                    ["等级"] = userInfo.Level,
                    ["签名"] = userInfo.Sign,
                    ["VIP状态"] = userInfo.Vip.VipStatus ? userInfo.Vip.VipType : "非大会员",
                    ["生日"] = userInfo.Birthday
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"获取简化用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取原始JSON响应（用于调试）
        /// </summary>
        public async Task<string> GetRawUserInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.bilibili.com/x/space/myinfo");
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取原始用户信息失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
