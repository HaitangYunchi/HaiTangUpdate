/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：
 * 命名空间：HaiTangUpdate
 * 唯一标识：a013030b-3ddb-449a-b414-13a9955a1e86
 * 文件名：HaiTangUpdate
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/4/13 16:48:29
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：海棠云螭
 * 时间：2025-06-15
 * 修改说明：优化了实例ID，OpenID，账户登录邮箱和密码相关输入错误直接报错的问题，
 * 采用验证式，直接返回错误信息或False，不会出现这些信息导致程序u崩溃
 *
 * 版本：V1.3.1-rc
 *----------------------------------------------------------------*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static HaiTangUpdate.JsonHelper;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HaiTangUpdate
{
    public class Update
    {
        #region 常量定义

        private const string Salt = "k3apRuJR2j388Yy5CWxfnXrHkwg3AvUntgVhuUMWBDXDEsyaeX7Ze3QbvmejbqSz"; //生成机器码用的加密盐值
        private readonly HttpClient _httpClient = new HttpClient();
        private const string DefaultApiUrl = "http://api.2018k.cn";
        private static string OpenApiUrl = DefaultApiUrl;
        // 可用的API地址列表，用于故障转移
        private static readonly string[] ApiAddressList =
        {
            "http://api.2018k.cn",
            "http://api2.2018k.cn",
            "http://api3.2018k.cn",
            "http://api4.2018k.cn"
        };
        // 用于存储当前API地址的索引
        private static int currentApiIndex = 0;
        // 记录每个API地址的健康状态和最后检测时间
        private static readonly Dictionary<string, ApiHealthStatus> apiHealthStatus = new Dictionary<string, ApiHealthStatus>();
        // 健康状态缓存时间（5分钟）
        private static readonly TimeSpan healthCacheDuration = TimeSpan.FromMinutes(5);
        // 锁对象，确保线程安全
        private static readonly object lockObject = new object();

        #endregion
        #region 公有方法
        /// <summary>
        /// 获取机器码 cpu+主板+64位盐值 进行验证
        /// </summary>
        /// <returns>string 返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX</returns>

        public string GetMachineCode()
        {
            try
            {
                // 获取硬件信息
                string cpuId = GetCpuId();
                string motherboardId = GetMotherboardId();
                // 生成机器码
                return GenerateFormattedCode(cpuId, motherboardId);
            }
            catch (Exception ex)
            {
                return GenerateErrorCode(); // 如果失败生成错误码 这种几率几乎可以忽略不计
            }
        }
        /// <summary>
        /// 获取机器码 cpu+主板+64位盐值 进行验证
        /// </summary>
        /// <returns>string 返回128字符串机器码</returns>
        public string GetMachineCodeEx()
        {
            try
            {
                // 获取硬件信息
                string cpuId = GetCpuId();
                string motherboardId = GetMotherboardId();
                // 生成机器码
                return GenerateFormattedCodeEx(cpuId, motherboardId);   // 获取512位 128字符串的机器码
            }
            catch (Exception ex)
            {
                return GenerateErrorCode(); // 如果失败生成错误码 这种几率几乎可以忽略不计
            }
        }
        /// <summary>
        /// 检测实例是否正常 （ 程序实例ID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>返回布尔值 如果 Code 为空，机器码为空时，使用自带的机器码</returns>
        public async Task<bool> GetSoftCheck(string ID, string key, string Code = null)
        {
            string _result;
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx();
            }
            _result = await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        return "false";
                    }
                    else 
                    {
                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        Json _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        try
                        {
                            // 尝试解密数据，失败则直接返回 false
                            string JsonData = AesDecrypt(_JsonData.data, key);
                            Json _Data = JsonConvert.DeserializeObject<Json>(JsonData);
                            return _Data.User != null ? "true" : "false";
                        }
                        catch
                        {
                            return "false";
                        }
                    }

                    
                }
            });

            return bool.TryParse(_result, out bool result) && result; // 解析失败也返回 false
        }
        /// <summary>
        /// 获取软件全部信息 （ 程序实例ID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>返回 Json 如果 Code 为空，机器码为空时，使用自带的机器码</returns>
        public async Task<string> GetUpdate(string ID, string key,string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx();
            }
            bool _Check = await GetSoftCheck(ID, key,Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        //string JsonData = AesDecrypt(_JsonData.data, key);
                        string JsonData = AesDecrypt(_JsonData.data, key);
                        

                        try
                        {
                            // 尝试将响应内容解析为 JSON 对象并格式化
                            var jsonObject = JsonConvert.DeserializeObject(JsonData);
                            return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                            //return jsonObject.ToString();
                        }
                        catch
                        {
                            // 如果解析失败，返回原始内容
                            return JsonData;
                        }

                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件全部信息失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件全部信息时出错: {ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 获取软件实例ID （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回实例ID，机器码可空</returns>
        public async Task<string> GetSoftwareID(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx();
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.SoftwareID;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件实例ID失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件实例ID时出错: {ex.Message}");
                    }
                }
            });
     
        }
        /// <summary>
        /// 获取软件版本 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件版本号，机器码可空</returns>
        public async Task<string> GetVersionNumber(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx();
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.VersionNumber;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件版本失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件版本时出错: {ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 获取软件名称 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件名称，机器码可空</returns>
        public async Task<string> GetSoftwareName(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx();
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.SoftwareName;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件名称失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件名称时出错: {ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 获取软件更新内容 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件更新信息，机器码可空</returns>
        public async Task<string> GetVersionInformation(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.VersionInformation;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件更新内容失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件更新内容时出错: {ex.Message}");
                    }
                }
            });  
        }
        /// <summary>
        /// 获取软件公告 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件公告信息，机器码可空</returns>
        public async Task<string> GetNotice(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.Notice;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件公告失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件公告时出错: {ex.Message}");
                    }
                }
            });     
        }
        /// <summary>
        /// 获取软件下载链接 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件下载链接，机器码可空</returns>
        public async Task<string> GetDownloadLink(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.DownloadLink;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件下载链接失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件下载链接时出错: {ex.Message}");
                    }
                }
            });   
        }
        /// <summary>
        /// 获取软件访问量 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件访问量数据 非实时，机器码可空</returns>
        public async Task<string> GetNumberOfVisits(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.NumberOfVisits;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件访问量失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件访问量时出错: {ex.Message}");
                    }
                }
            });    
        }
        /// <summary>
        /// 获取软件最低版本号 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>string 返回软件最低版本号，机器码可空</returns>
        public async Task<string> GetMiniVersion(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.MiniVersion;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件最低版本号失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件最低版本号时出错: {ex.Message}");
                    }
                }
            });  
        }
        /// <summary>
        /// 获取卡密状（ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>bool 返回卡密当前状态是否有效, 一般为判断软件是否注册 True  , False </returns>
        public async Task<bool> GetIsItEffective(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return false;
            }
            string response = await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.IsItEffective;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取卡密状失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理卡密状出错: {ex.Message}");
                    }
                }
            });
            if (response == "n")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 获取卡密过期时间戳 （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>string 返回软件卡密时间戳</returns>
        public async Task<string> GetExpirationDate(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            var _IsItEffective = await GetIsItEffective(ID, key, Code);
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        if (_IsItEffective == true && string.IsNullOrEmpty(result.ExpirationDate))
                        {

                            return "7258089599000";
                        }
                        else
                        {
                            return result.ExpirationDate;
                        }
                        
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取卡密过期时间戳失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理卡密过期时间戳出错: {ex.Message}");
                    }
                }
            });  
        }
        /// <summary>
        /// 获取卡密备注 （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>string 返回卡密备注</returns>
        public async Task<string> GetRemarks(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.NetworkVerificationRemarks;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取卡密备注失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理卡密备注数据时出错: {ex.Message}");
                    }
                }
            });   
        }
        /// <summary>
        /// 获取卡密有效期类型 （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>string 返回卡密有效期类型, 卡密有效期天数</returns>
        public async Task<string> GetNumberOfDays(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var _IsItEffective = await GetIsItEffective(ID, key, Code);
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        if (_IsItEffective == true && string.IsNullOrEmpty(result.NumberOfDays))
                        {

                            return "99999";
                        }
                        else
                        {
                            return result.NumberOfDays;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取卡密有效期失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理卡密有效期数据时出错: {ex.Message}");
                    }
                }
            });   
        }
        /// <summary>
        /// 获取卡密ID （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>string 返回卡密ID</returns>
        public async Task<string> GetNetworkVerificationId(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.NetworkVerificationId;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取卡密ID失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理卡密ID数据时出错: {ex.Message}");
                    }
                }
            });  
        }
        /// <summary>
        /// 获取服务器时间 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码 可空</param>
        /// <returns>string 返回服务器时间, 时间戳，机器码可空</returns>
        public async Task<string> GetTimeStamp(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={Uri.EscapeDataString(ID)}" + (string.IsNullOrEmpty(Code) ? "" : $"&machineCode={Uri.EscapeDataString(Code)}&isAPI=y");

                    try
                    {
                        // 发送GET请求并获取响应
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                        // 确保请求成功
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();

                        // 反序列化JSON
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string decryptedData = AesDecrypt(_JsonData.data, key);

                        // 反序列化解密后的数据
                        var result = JsonConvert.DeserializeObject<Json>(decryptedData);

                        return result.TimeStamp;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        // 记录HTTP请求错误
                        throw new Exception($"获取时间戳失败 - 网络请求错误: {httpEx.Message}");
                    }
                    catch (JsonException jsonEx)
                    {
                        // 记录JSON解析错误
                        throw new Exception($"获取时间戳失败 - 数据解析错误: {jsonEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 其他错误处理
                        throw new Exception($"获取时间戳时发生未知错误: {ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 获取软件是否强制更新 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码 可空</param>
        /// <returns>bool 返回软件是否强制更新，机器码可空</returns>
        public async Task<bool> GetMandatoryUpdate(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return false;
            }
            string response = await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.MandatoryUpdate;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件MD5失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件MD5数据时出错: {ex.Message}");
                    }
                }
            });
            if (response == "n")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 获取软件MD5 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码 可空</param>
        /// <returns>string 返回软件MD5，机器码可空</returns>
        public async Task<string> GetSoftwareMd5(string ID, string key, string Code = null)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _Check = await GetSoftCheck(ID, key, Code);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建请求URL
                    string requestUrl = $"{apiUrl}/v3/obtainSoftware?softwareId={ID}&machineCode={Code}&isAPI=y";

                    try
                    {
                        // 发送GET请求
                        HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                        response.EnsureSuccessStatusCode();

                        // 读取响应内容
                        string jsonString = await response.Content.ReadAsStringAsync();
                        var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                        // 解密数据
                        string JsonData = AesDecrypt(_JsonData.data, key);

                        // 反序列化最终结果
                        var result = JsonConvert.DeserializeObject<Json>(JsonData);
                        return result.SoftwareMd5;
                    }
                    catch (HttpRequestException ex)
                    {
                        // 处理HTTP请求异常
                        throw new Exception($"获取软件MD5失败: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        throw new Exception($"处理软件MD5数据时出错: {ex.Message}");
                    }
                }
            });
        }
        /// <summary>
        /// 获取云变量 （ 程序实例ID，OpenID，云端变量名称 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="VarName">云端变量名称</param>
        /// <returns>string 返回云变量的值</returns>
        public async Task<string> GetCloudVariables(string ID, string key, string VarName)
        {
            bool _Check = await GetSoftCheck(ID, key);
            if (_Check == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                using (HttpClient httpClient = new())
                {
                    // 构建API请求URL
                    string requestUrl = $"{apiUrl}/v3/getCloudVariables?softwareId={ID}&isAPI=y";

                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    // 确保请求成功
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);
                    // 解密数据
                    string JsonData = AesDecryptData(_JsonData.data, key);

                    // 解析JSON数组
                    JArray jsonArray = JArray.Parse(JsonData);
                    List<KeyValuePair<string, string>> configList = new List<KeyValuePair<string, string>>();

                    // 遍历JSON数据
                    foreach (JObject item in jsonArray)
                    {
                        string CloudKey = item["key"].ToString();
                        string CloudValue = item["value"].ToString();
                        configList.Add(new KeyValuePair<string, string>(CloudKey, CloudValue));
                    }

                    // 查找指定变量名
                    var _Var = configList.FirstOrDefault(p => p.Key == VarName);
                    string CloudVar = _Var.Value;
                    return CloudVar;
                }
            });
        }
        /// <summary>
        /// 激活软件  （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="authId">卡密ID</param>
        /// <param name="Code">机器码</param>
        /// <returns>返回JSON</returns>
        public async Task<string> ActivationKey(string authId, string ID, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                string url = $"{apiUrl}/v3/activation?authId={authId}&softwareId={ID}&machineCode={Code}&isAPI=y";
                // 发送 GET 请求
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            });
        }
        /// <summary>
        /// 发送消息  （ 程序实例ID，要发送的消息 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="message">要发送的消息</param>
        /// <returns>不返回消息</returns>
        public async Task<string> MessageSend(string ID, string message)
        {
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                message = Uri.EscapeDataString(message);
                string url = $"{apiUrl}/v3/messageSend?softwareId={ID}&message={message}&isAPI=y";
                // 发送 GET 请求
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                // 获取响应内容并格式化
                string responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // 尝试将响应内容解析为 JSON 对象并格式化
                    var jsonObject = JsonConvert.DeserializeObject(responseContent);
                    return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                }
                catch
                {
                    // 如果解析失败，返回原始内容
                    return responseContent;
                }
            });
        }

        /// <summary>
        /// 创建卡密  （ 卡密天数，卡密备注，程序实例ID，OpenID ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="day">卡密天数</param>
        /// <param name="remark">卡密备注</param>
        /// <returns>返回JSON</returns>
        public async Task<string> CreateNetworkAuthentication(int day, string remark, string ID, string key)
        {
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                // 构建请求数据
                var data = new
                {
                    day,
                    remark,
                    times = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds
                };

                // 加密数据
                string encodedCiphertext = AesEncrypt(data, key);

                // 发送请求
                string url = $"{apiUrl}/v3/createNetworkAuthentication?info={Uri.EscapeDataString(encodedCiphertext)}&softwareId={ID}&isAPI=y";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 获取响应内容并格式化
                string responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // 尝试将响应内容解析为 JSON 对象并格式化
                    var jsonObject = JsonConvert.DeserializeObject(responseContent);
                    return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                }
                catch
                {
                    // 如果解析失败，返回原始内容
                    return responseContent;
                }
            });
        }
        /// <summary>
        /// 解绑、换绑  （ 程序实例ID，OpenID，卡密ID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="AuthId">卡密ID</param>
        /// <param name="Code">机器码</param>
        /// <returns>返回JSON</returns>
        public async Task<string> ReplaceBind(string ID, string key, string AuthId, string Code = null)
        {
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                // 构建请求数据
                var data = new
                {
                    authId = AuthId,
                    machineCode = Code
                };

                // 加密数据
                string encodedCiphertext = AesEncrypt(data, key);
                // 发送请求
                string url = $"{apiUrl}/v3/replaceBind?softwareId={ID}&info={Uri.EscapeDataString(encodedCiphertext)}&isAPI=y";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 获取响应内容并格式化
                string responseContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // 尝试将响应内容解析为 JSON 对象并格式化
                    var jsonObject = JsonConvert.DeserializeObject(responseContent);
                    return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                }
                catch
                {
                    // 如果解析失败，返回原始内容
                    return responseContent;
                }
            });
        }
        /// <summary>
        /// 获取剩余使用时间  （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>长整数类型long 永久返回-1，过期返回0，未注册返回1，其余返回时间戳，</returns>
        public async Task<long> GetRemainingUsageTime(string ID, string key, string Code)
        {
            if (string.IsNullOrEmpty(Code))
            {
                Code = GetMachineCodeEx(); // 判断机器码是否为空，为空使用默认机器码
            }
            bool _IsItEffective = await GetIsItEffective(ID, key,Code);
            //string _numberOfDays = await GetNumberOfDays(ID, key, Code);
            string _expirationDate = await GetExpirationDate(ID, key, Code);
            
            try
            {
                if (_IsItEffective == true && _expirationDate == "7258089599000")
                {
                    return -1;
                }
                else if (_IsItEffective == true && !string.IsNullOrWhiteSpace(_expirationDate))
                {
                    long lastTimestamp = long.Parse(_expirationDate);
                    long currentTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    long timestamp = (lastTimestamp - currentTimestamp);
                    if (timestamp > 0)
                    {
                        return timestamp;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 1;
                }
            }
            catch 
            {
                return 0;
            }

        }

        /// <summary>
        /// 获取网络验证码  （ 程序实例ID，OpenID ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <returns>string 返回验证码</returns>
        public async Task<string> GetNetworkCode(string ID, string key)
        {
            bool _logon = await GetSoftCheck(ID, key);
            if (_logon == false)
            {
                return _error;
            }
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID
                };
                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl+ "/v3/captcha", content);              
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);
                    string JsonData = AesDecryptData(_JsonData.data, key);


                    return JsonData;
                    
                }
            });
        }
        /// <summary>
        /// 用户注册  （ 程序实例ID，邮箱，密码，昵称，验证码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <param name="avatarUrl">用户头像</param>
        /// <param name="nickName">昵称</param>
        /// <param name="captcha">验证码</param>
        /// <returns>返回 布尔类型 True 或 Fales 【昵称，头像，验证码】可空</returns>
        public async Task<bool> CustomerRegister(string ID,string email, string password,string nickName = null, string avatarUrl = null, string captcha = null)
        {

            string _data = await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password, 
                    avatarUrl = avatarUrl, 
                    nickName = nickName, 
                    captcha = captcha

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerRegister", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);
                    string JsonData =_JsonData.Success.ToString();
                    return JsonData;
                  }
            });
            return bool.TryParse(_data, out var result) && result;
        }
        /// <summary>
        /// 用户登录  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回布尔类型 bool</returns>
        public async Task<bool> CustomerLogon(string ID,string key, string email, string password)
        {
            string _result;
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _result = await ExecuteApiRequest(async (apiUrl) =>
                {
                    var requestData = new
                    {
                        softwareId = ID,
                        email = email,
                        password = password,
                        timeStamp = timestamp

                    };

                    // 序列化为 JSON
                    string json = JsonSerializer.Serialize(requestData);

                    // 使用 HttpClient 发送 POST 请求
                    using (HttpClient client = new HttpClient())
                    {
                        // 设置请求头（Content-Type）
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                        // 构造 StringContent（请求体）
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        // 发送 POST 请求
                        HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"请求失败！HTTP状态码: {response.StatusCode}");
                        }
                        string responseBody = await response.Content.ReadAsStringAsync();
                        JsonUser result = JsonConvert.DeserializeObject<JsonUser>(responseBody);
                        // 检查登录是否成功
                        if (result == null || result.Success == false || result.Data == null || result.Data.CustomerId == null)
                        {
                            string errorMsg = (result?.Message ?? "未知错误");
                            return  "false";
                            throw new Exception(errorMsg);
                        }
                        string decryptedData = AesDecryptData(result.Data.TimeCrypt, key);
                        string okMsg = (result?.Message);
                        string JsonData = result.Success.ToString();
                        return JsonData;

                    }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"程序异常: {ex.Message}");
            }
            return bool.TryParse(_result, out bool result) && result; // 解析失败也返回 false
        }

        /// <summary>
        /// 获取用户所有信息  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回JSON</returns>
        public async Task<string> GetUserInfo(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    string result = JsonConvert.SerializeObject(_JsonData.Data, Formatting.Indented);
                    return result;

                }
            });
        }
        /// <summary>
        /// 获取用户ID  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回string类型</returns>
        public async Task<string> GetUserId(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);                   
                    string result = _JsonData.Data.CustomerId;
                    return result;

                }
            });
        }
        /// <summary>
        /// 获取用户头像  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回string类型</returns>
        public async Task<string> GetUserAvatar(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    string result = _JsonData.Data.AvatarUrl;
                    return result;

                }
            });
        }
        /// <summary>
        /// 获取用户昵称  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回string类型</returns>
        public async Task<string> GetUserNickname(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    string result = _JsonData.Data.Nickname;
                    return result;

                }
            });
        }
        /// <summary>
        /// 获取用户邮箱  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回string类型</returns>
        public async Task<string> GetUserEmail(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    string result = _JsonData.Data.Email;
                    return result;

                }
            });
        }
        /// <summary>
        /// 获取账户剩余时长  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回string类型</returns>
        public async Task<string> GetUserBalance(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    string result =_JsonData.Data.Balance.ToString();
                    return result;

                }
            });
        }
        /// <summary>
        /// 是否授权  （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>返回布尔类型</returns>
        public async Task<bool> GetUserLicense(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _logon;
            }
            string dataJson;
            string _data;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _data= await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    dataJson = _JsonData.Data.License;
                    if (dataJson == "y")
                    {
                        return dataJson;
                    }
                    else 
                    {
                        return dataJson;
                    }
                }
            });
            return bool.TryParse(_data, out var result) && result;
        }
        /// <summary>
        /// 获取用户登录时间戳 （ 程序实例ID，OpenID,邮箱，密码）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID/param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>string 返回时间戳</returns>
        public async Task<string> GetUserTimeCrypt(string ID, string key, string email, string password)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    softwareId = ID,
                    email = email,
                    password = password,
                    timeStamp = timestamp

                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerLogin", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<JsonUser>(jsonString);
                    // 解密数据
                    string JsonData = AesDecryptData(_JsonData.Data.TimeCrypt, key);
                    string dataJson = JsonData;
                    return dataJson;
                }    
            });
        }
        /// <summary>
        /// 卡密充值  （ 程序实例ID，OpenID,登录邮箱，密码，卡密ID ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="email">登录邮箱</param>
        /// <param name="password">登录密码</param>
        /// <param name="AuthId">卡密ID</param>
        /// <returns>string 返回验证码</returns>
        public async Task<string> Recharge(string ID, string key, string email, string password, string AuthId)
        {
            bool _logon = await CustomerLogon(ID, key, email, password);
            if (_logon == false)
            {
                return _worring;
            }
            var _customerId = await GetUserId(ID, key, email, password);         
            return await ExecuteApiRequest(async (apiUrl) =>
            {
                var requestData = new
                {
                    customerId = _customerId.ToString(),
                    authId = AuthId
                };

                // 序列化为 JSON
                string json = JsonSerializer.Serialize(requestData);

                // 使用 HttpClient 发送 POST 请求
                using (HttpClient client = new HttpClient())
                {
                    // 设置请求头（Content-Type）
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    // 构造 StringContent（请求体）
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 发送 POST 请求
                    HttpResponseMessage response = await client.PostAsync(apiUrl + "/v3/customerRecharge", content);
                    string jsonString = await response.Content.ReadAsStringAsync();
                    //var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);
                    return jsonString;

                }
            });
        }

        public string AesEncrypt(object data, string key)
        {
            // 将数据转换为JSON字符串
            string plaintext = JsonConvert.SerializeObject(data);

            // 使用AES加密
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = HexStringToByteArray(key);
                aesAlg.IV = new byte[16]; // 16字节全零IV
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // 创建加密器
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // 加密数据
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plaintext);
                        }
                        byte[] encrypted = msEncrypt.ToArray();

                        // 转换为Base64字符串
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
        }
        public string AesDecrypt(string encryptedData, string key)
        {

            try
            {
                // 将Base64密文转换为字节数组
                byte[] cipherBytes = Convert.FromBase64String(encryptedData);

                // 创建AES解密器
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = HexStringToByteArray(key); ;
                    aesAlg.IV = new byte[16];
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    // 创建解密器
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    // 执行解密
                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                // 返回解密后的UTF8字符串
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)    
            {
               return ($"程序异常: {ex.Message}");
            }

        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查网络连接是否可用
        /// </summary>
        /// <returns>如果网络可用返回true，否则返回false</returns>
        private static bool IsNetworkAvailable()
        {
            try
            {
                // 使用NetworkInterface检查网络连接状态
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                // 如果检查过程中出现异常，保守返回false
                return false;
            }
        }

        // API健康状态类
        private class ApiHealthStatus
        {
            public bool IsHealthy { get; set; } = true;
            public DateTime LastChecked { get; set; } = DateTime.MinValue;
            public Exception LastError { get; set; }
        }

        /// <summary>
        /// 初始化健康状态字典
        /// </summary>
        static Update()
        {
            foreach (var apiUrl in ApiAddressList)
            {
                apiHealthStatus[apiUrl] = new ApiHealthStatus();
            }
        }

        /// <summary>
        /// 获取当前可用的最佳API地址
        /// </summary>
        private static string GetBestAvailableApiUrl()
        {
            lock (lockObject)
            {
                // 首先检查网络是否可用
                if (!IsNetworkAvailable())
                {
                    return DefaultApiUrl;
                }
                // 首先检查当前地址是否健康
                if (IsApiHealthy(OpenApiUrl))
                {
                    return OpenApiUrl;
                }

                // 当前地址不健康，寻找下一个健康地址
                for (int i = 0; i < ApiAddressList.Length; i++)
                {
                    var index = (currentApiIndex + i + 1) % ApiAddressList.Length;
                    var apiUrl = ApiAddressList[index];

                    if (IsApiHealthy(apiUrl))
                    {
                        currentApiIndex = index;
                        OpenApiUrl = apiUrl;
                        return apiUrl;
                    }
                }

                // 所有备用地址都不健康，回退到默认地址
                OpenApiUrl = DefaultApiUrl;
                return DefaultApiUrl;
            }
        }

        /// <summary>
        /// 检查API地址是否健康（带缓存）
        /// </summary>
        private static bool IsApiHealthy(string apiUrl)
        {
            // 默认地址总是被认为是健康的
            if (apiUrl == DefaultApiUrl)
            {
                return true;
            }

            var status = apiHealthStatus[apiUrl];

            // 如果缓存未过期，直接返回缓存状态
            if (DateTime.Now - status.LastChecked < healthCacheDuration)
            {
                return status.IsHealthy;
            }

            // 需要重新检测（实际检测逻辑可以在后台线程中执行）
            // 这里为了简化，我们假设API是健康的，实际使用时可以实现主动检测
            return true;
        }

        /// <summary>
        /// 标记API地址为不健康
        /// </summary>
        private static void MarkApiAsUnhealthy(string apiUrl, Exception error)
        {
            if (apiUrl == DefaultApiUrl) return;

            lock (lockObject)
            {
                var status = apiHealthStatus[apiUrl];
                status.IsHealthy = false;
                status.LastError = error;
                status.LastChecked = DateTime.Now;
            }
        }

        /// <summary>
        /// 执行API请求，使用最佳可用地址
        /// </summary>
        private static async Task<string> ExecuteApiRequest(Func<string, Task<string>> requestFunc)
        {
            Exception lastException = null;
            string bestApiUrl = GetBestAvailableApiUrl();

            try
            {
                // 使用最佳可用地址执行请求
                return await requestFunc(bestApiUrl);
            }
            catch (HttpRequestException ex)
            {
                if (IsNetworkAvailable())
                {
                    lastException = ex;
                    // 标记当前地址为不健康
                    MarkApiAsUnhealthy(bestApiUrl, ex);

                    // 尝试使用下一个可用地址重试一次
                    bestApiUrl = GetBestAvailableApiUrl();
                    if (bestApiUrl != OpenApiUrl) // 确保不是同一个地址
                    {
                        try
                        {
                            return await requestFunc(bestApiUrl);
                        }
                        catch (Exception retryEx)
                        {
                            lastException = retryEx;
                            MarkApiAsUnhealthy(bestApiUrl, retryEx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            throw new Exception($"API请求失败。最后错误: {lastException?.Message}", lastException);
        }

        // 辅助方法：将十六进制字符串转换为字节数组
        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符串长度必须是偶数");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // 获取CPU信息
        private static string GetCpuId()
        {
            return GetWmiInfo("Win32_Processor", "ProcessorId") ?? "NA";
        }
        // 获取主板信息
        private static string GetMotherboardId()
        {
            return GetWmiInfo("Win32_BaseBoard", "SerialNumber") ?? "NA";
        }

        private static string GetWmiInfo(string className, string propertyName)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string value = mo[propertyName]?.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // 过滤无效值（某些主板返回空或占位符）
                        if (value.ToUpper() == "NONE" ||
                            value.ToUpper() == "TO BE FILLED BY O.E.M.")
                            continue;

                        return value;
                    }
                }
            }
            catch (ManagementException ex)
            {

            }
            return null;
        }
        // 生成序列号
        private static string GenerateFormattedCode(string cpuId, string motherboardId)
        {
            // 组合硬件信息
            string composite = $"{cpuId}_{motherboardId}_{Salt}";
            // 格式化输出
            return FormatMachineCode(ShaHasher.Sha256(composite));

        }
        // 格式化机器码
        private static string FormatMachineCode(string hash)
        {
            // 确保20字符长度
            hash = hash.Length >= 20 ?
                   hash.Substring(0, 20) :
                   hash.PadRight(20, '0');

            // 5字符分段格式化
            return $"{hash.Substring(0, 5)}-{hash.Substring(5, 5)}-{hash.Substring(10, 5)}-{hash.Substring(15, 5)}";
        }
        // 如果生成机器码失败，则生成带时间戳的错误码（示例：ERR-2025-0329-ABCDE）  这个就是不必要担心，
        // 毕竟cpu+主板怎么可能两个都失败
        private static string GenerateFormattedCodeEx(string cpuId, string motherboardId)
        {
            // 组合硬件信息
            string composite = $"{cpuId}-{motherboardId}-{Salt}";

            return ShaHasher.Sha512(composite);
        }
       
        private static string GenerateErrorCode()
        {

            string timestamp = DateTime.Now.ToString("yyyy-MMdd-HHmm");
            return $"ERR-{timestamp.Substring(0, 9)}-{Guid.NewGuid().ToString("N").Substring(0, 5)}";
        }


        private string AesDecryptData(string encryptedText, string secret)
        {
            // Base64解码
            byte[] cipherData = Convert.FromBase64String(encryptedText);
            if (cipherData.Length < 16)
                throw new ArgumentException("Invalid encrypted text");

            // 提取salt（8字节，从索引8开始）
            byte[] saltData = new byte[8];
            Array.Copy(cipherData, 8, saltData, 0, 8);

            // 生成密钥和IV
            GenerateKeyAndIV(saltData, Encoding.Default.GetBytes(secret), out byte[] key, out byte[] iv);

            // 提取加密数据（从第16字节开始）
            byte[] data = new byte[cipherData.Length - 16];
            Array.Copy(cipherData, 16, data, 0, data.Length);

            // AES解密
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream ms = new MemoryStream(data))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                {
                    // 将UTF8解码结果转换为系统默认编码
                    var utf8Result = sr.ReadToEnd();
                    byte[] ansiBytes = Encoding.Default.GetBytes(utf8Result);
                    var json = Encoding.Default.GetString(ansiBytes);
                    var parsedJson = JsonConvert.DeserializeObject(json);
                    return JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                }
            }
        }
        private void GenerateKeyAndIV(byte[] saltData, byte[] password, out byte[] key, out byte[] iv)
        {
            StringBuilder str = new StringBuilder();
            string md5str = "";

            // 三次MD5迭代
            for (int i = 0; i < 3; i++)
            {
                // 组合前次MD5结果+密码+salt
                byte[] previousMd5 = HexStringToBytes(md5str);
                byte[] combined = CombineBytes(previousMd5, password, saltData);

                // 计算MD5
                using (MD5 md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(combined);
                    md5str = BytesToHexString(hash);
                    str.Append(md5str);
                }
            }

            // 生成最终字节数组
            byte[] resultBytes = HexStringToBytes(str.ToString());

            // 提取密钥和IV
            key = new byte[32];
            iv = new byte[16];
            Array.Copy(resultBytes, 0, key, 0, 32);
            Array.Copy(resultBytes, 32, iv, 0, 16);
        }
        // 辅助函数：十六进制字符串转字节数组
        private byte[] HexStringToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hexadecimal string must have even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        // 辅助函数：字节数组转十六进制字符串
        private string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
        // 辅助函数：组合多个字节数组
        private byte[] CombineBytes(params byte[][] arrays)
        {
            int length = 0;
            foreach (byte[] array in arrays)
                length += array.Length;

            byte[] combined = new byte[length];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Array.Copy(array, 0, combined, offset, array.Length);
                offset += array.Length;
            }
            return combined;
        }
        #endregion

    }
}
