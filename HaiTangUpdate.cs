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
 * 修改人：
 * 时间：
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Management;
using Newtonsoft.Json.Linq;
using System.Net;
using JsonException = System.Text.Json.JsonException;
using System.Net.Http;

namespace HaiTangUpdate
{
    public class Update
    {
        private const string Salt = "k3apRuJR2j388Yy5CWxfnXrHkwg3AvUntgVhuUMWBDXDEsyaeX7Ze3QbvmejbqSz";
        private readonly HttpClient _httpClient = new HttpClient();
        private string OpenApiUrl = "https://api.2018k.cn/v3/";

        public class Json
        {
            [JsonPropertyName("code")] // 如果使用System.Text.Json则用 [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("data")]
            public string Data { get; set; }

            [JsonPropertyName("softwareID")]
            public string SoftwareID { get; set; }

            [JsonPropertyName("versionNumber")]
            public string VersionNumber { get; set; }

            [JsonPropertyName("softwareName")]
            public string SoftwareName { get; set; }

            [JsonPropertyName("versionInformation")]
            public string VersionInformation { get; set; }

            [JsonPropertyName("notice")]
            public string Notice { get; set; }

            [JsonPropertyName("downloadLink")]
            public string DownloadLink { get; set; }

            [JsonPropertyName("numberOfVisits")]
            public string NumberOfVisits { get; set; }

            [JsonPropertyName("miniVersion")]
            public string MiniVersion { get; set; }

            [JsonPropertyName("isItEffective")]
            public string IsItEffective { get; set; }

            [JsonPropertyName("expirationDate")]
            public string ExpirationDate { get; set; }

            [JsonPropertyName("networkVerificationRemarks")]
            public string NetworkVerificationRemarks { get; set; }

            [JsonPropertyName("numberOfDays")]
            public string NumberOfDays { get; set; }

            [JsonPropertyName("networkVerificationId")]
            public string NetworkVerificationId { get; set; }

            [JsonPropertyName("timeStamp")]
            public string TimeStamp { get; set; }

            [JsonPropertyName("mandatoryUpdate")]
            public string MandatoryUpdate { get; set; }

            [JsonPropertyName("softwareMd5")]
            public string SoftwareMd5 { get; set; }

            [JsonPropertyName("CloudVariables")]
            public string CloudVariables { get; set; }
        }
        #region 加密解密
        /// <summary>
        /// 解密data数据 (加密数据,OpenID)
        /// </summary>
        /// <returns>string 返回字符串格式</returns> 
        public string AesDecrypt(string encryptedText, string secret)
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
        /// <summary>
        /// 加密data数据 (待加密数据,OpenID)
        /// </summary>
        /// <returns>string 返回Base64字符串格式</returns>
        public string AesEncrypt(string text, string OpenID)
        {
            // 生成随机头（8字节）和盐（8字节）
            byte[] header = GenerateRandomBytes(8);
            byte[] salt = GenerateRandomBytes(8);

            // 生成密钥和IV
            GenerateKeyAndIV(salt, Encoding.Default.GetBytes(OpenID), out byte[] key, out byte[] iv);

            // 加密数据
            byte[] encryptedData = EncryptString(text, key, iv);

            // 构建完整字节数组：[8头] + [8盐] + [加密数据]
            byte[] cipherData = CombineBytes(header, salt, encryptedData);

            return Convert.ToBase64String(cipherData);
        }
        private byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
#pragma warning disable SYSLIB0023
            using (RNGCryptoServiceProvider rng = new())
            {
                rng.GetBytes(bytes);
            }
#pragma warning restore SYSLIB0023
            return bytes;
        }
        private byte[] EncryptString(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 注意编码转换：明文转UTF8
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        public string EncryptData(object data, string key)
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
        public string DecryptData(string encryptedData, string key)
        {
            // 将Base64字符串转换为字节数组
            byte[] cipherText = Convert.FromBase64String(encryptedData);

            // 使用AES解密
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = HexStringToByteArray(key);
                aesAlg.IV = new byte[16]; // 16字节全零IV，必须与加密时一致
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                // 创建解密器
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // 解密数据
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // 读取解密后的字符串
                            var decrypted = srDecrypt.ReadToEnd();
                            var parsedJson = JsonConvert.DeserializeObject(decrypted);
                            return JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                        }
                    }
                }
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        #endregion
        #region 获取机器码(CPU+主板)
        /// <summary>
        /// 获取机器码 cpu+主板 进行验证
        /// </summary>
        /// <returns>返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX</returns>
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
                return GenerateErrorCode(); // 生成错误码
            }
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

            // 生成哈希
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(composite));

                // 转换为大写十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("X2"));
                }

                // 格式化输出
                return FormatMachineCode(sb.ToString());
            }
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
        private static string GenerateErrorCode()
        {

            string timestamp = DateTime.Now.ToString("yyyy-MMdd-HHmm");
            return $"ERR-{timestamp.Substring(0, 9)}-{Guid.NewGuid().ToString("N").Substring(0, 5)}";
        }
        #endregion

        /// <summary>
        /// 获取软件全部信息 （ 程序实例ID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="Code">机器码，可以省略</param>
        /// <returns>如果 Code 为空，机器码为空时，只返回程序基本信息</returns>
        public async Task<string> GetUpdate(string ID, string key,string Code = null)
        {
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

                    try
                    {
                        // 尝试将响应内容解析为 JSON 对象并格式化
                        var jsonObject = JsonConvert.DeserializeObject(JsonData);
                        return JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using(HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            
        }
        /// <summary>
        /// 获取卡密状（ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码</param>
        /// <returns>string 返回卡密当前状态是否有效, 一般为判断软件是否注册 True (y) , False (n)</returns>
        public async Task<string> GetIsItEffective(string ID, string key, string Code)
        {
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

                    // 反序列化最终结果
                    var result = JsonConvert.DeserializeObject<Json>(JsonData);
                    return result.ExpirationDate;
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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

                    // 反序列化最终结果
                    var result = JsonConvert.DeserializeObject<Json>(JsonData);
                    return result.NumberOfDays;
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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={Uri.EscapeDataString(ID)}" +(string.IsNullOrEmpty(Code) ? "" : $"&machineCode={Uri.EscapeDataString(Code)}");

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
                    string decryptedData = AesDecrypt(_JsonData.Data, key);

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
        }
        /// <summary>
        /// 获取软件是否强制更新 （ 程序实例ID，OpenID，机器码 [null] ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="Code">机器码 可空</param>
        /// <returns>string 返回软件是否强制更新，机器码可空</returns>
        public async Task<string> GetMandatoryUpdate(string ID, string key, string Code = null)
        {           
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            using (HttpClient httpClient = new())
            {
                // 构建请求URL
                string requestUrl = $"{OpenApiUrl}obtainSoftware?softwareId={ID}&machineCode={Code}";

                try
                {
                    // 发送GET请求
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);

                    // 解密数据
                    string JsonData = AesDecrypt(_JsonData.Data, key);

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
            // 使用 HttpClient 替代已过时的 WebClient
            using (HttpClient httpClient = new())
            {
                // 构建API请求URL
                string apiUrl = $"{OpenApiUrl}getCloudVariables?softwareId={ID}";

                // 发送GET请求
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                // 确保请求成功
                response.EnsureSuccessStatusCode();

                // 读取响应内容
                string jsonString = await response.Content.ReadAsStringAsync();
                var _JsonData = JsonConvert.DeserializeObject<Json>(jsonString);
                // 解密数据
                string JsonData = AesDecrypt(_JsonData.Data, key);

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
        }
        /// <summary>
        /// 激活软件  （ 程序实例ID，OpenID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="authId">卡密ID</param>
        /// <param name="Code">机器码</param>
        /// <returns>不返回消息</returns>
        public async Task<string> ActivationKey(string authId, string ID, string Code)
        {
            string url = $"{OpenApiUrl}activation?authId={authId}&softwareId={ID}&machineCode={Code}";
            // 发送 GET 请求
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// 发送消息  （ 程序实例ID，要发送的消息 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="message">要发送的消息</param>
        /// <returns>不返回消息</returns>
        public async Task<string> MessageSend(string ID, string message)
        {
            message = Uri.EscapeDataString(message);
            string url = $"{OpenApiUrl}messageSend?softwareId={ID}&message={message}";
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
            // 构建请求数据
            var data = new
            {
                day,
                remark,
                times = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds
            };

            // 加密数据
            string encodedCiphertext = EncryptData(data, key);

            // 发送请求
            string url = $"{OpenApiUrl}createNetworkAuthentication?info={Uri.EscapeDataString(encodedCiphertext)}&softwareId={ID}&isAPI=y";

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
        }
        /// <summary>
        /// 创建卡密  （ 程序实例ID，OpenID，卡密ID，机器码 ）
        /// </summary>
        /// <param name="ID">程序实例ID</param>
        /// <param name="key">OpenID</param>
        /// <param name="authId">卡密ID</param>
        /// <param name="Code">机器码</param>
        /// <returns>返回JSON</returns>
        public async Task<string> ReplaceBind(string ID, string key, string authId, string Code)
        {
            // 构建请求数据
            var data = new
            {
                authId,
                machineCode = Code
            };

            // 加密数据
            string encodedCiphertext = EncryptData(data, key);
            // 发送请求
            string url = $"{OpenApiUrl}replaceBind?softwareId={ID}&info={Uri.EscapeDataString(encodedCiphertext)}&isAPI=y";

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
        }
    }
}
