/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library
 * 唯一标识：625f2ad1-862f-439f-8f74-cccf7cf7496f
 * 文件名：JsonConfigManager
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/29 5:24:04
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


using HaiTang.library.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaiTang.library.up2018
{
    /// <summary>
    /// JSON配置管理类
    /// </summary>
    public class JsonConfigManager
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonConfigManager(string filePath = "appsettings.json")
        {
            _filePath = filePath;

            // 配置JSON序列化选项，支持中文显示
            _jsonOptions = new JsonSerializerOptions
            {
                // 设置编码器，支持中文
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // 使用驼峰命名法
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // 缩进格式，便于阅读
                WriteIndented = true,
                // 允许尾随逗号
                AllowTrailingCommas = true,
                // 忽略空值
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };
        }
        /// <summary>
        /// 完整的应用配置模型
        /// </summary>
        public class AppSettingsModel
        {
            public MysoftConfig Mysoft { get; set; } = new MysoftConfig();
        }

        // <summary>
        /// 从源JSON转换并创建配置
        /// </summary>
        /// <param name="sourceJson">源JSON字符串</param>
        /// <returns>转换后的配置对象</returns>
        public AppSettingsModel ConvertFromSourceJson(string sourceJson)
        {
            try
            {
                // 反序列化源JSON
                var sourceModel = JsonSerializer.Deserialize<JsonMode>(sourceJson, _jsonOptions);
                if (sourceModel == null)
                    throw new ArgumentException("源JSON格式不正确");

                // 创建目标配置
                var appSettings = new AppSettingsModel();

                // 转换Mysoft配置
                appSettings.Mysoft = ConvertToMysoftConfig(sourceModel);

                return appSettings;
            }
            catch (Exception ex)
            {
                throw new Exception($"JSON转换失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 只更新Mysoft配置，保留其他所有配置
        /// </summary>
        /// <param name="sourceJson">源JSON字符串</param>
        public async Task UpdateOnlyMysoftFromSourceAsync(string sourceJson)
        {
            try
            {
                // 读取现有配置
                var existingConfig = await ReadConfigAsync();

                // 反序列化源JSON
                var sourceModel = JsonSerializer.Deserialize<JsonMode>(sourceJson, _jsonOptions);
                if (sourceModel == null)
                    throw new ArgumentException("源JSON格式不正确");

                // 只更新Mysoft配置
                existingConfig.Mysoft = ConvertToMysoftConfig(sourceModel);

                // 写入更新后的配置
                await WriteConfigAsync(existingConfig);
            }
            catch (Exception ex)
            {
                throw new Exception($"更新Mysoft配置失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 转换源数据到Mysoft配置
        /// </summary>
        private MysoftConfig ConvertToMysoftConfig(JsonMode source)
        {
            long _expriationDate;
            int _numberOfDays;
            if (source.isItEffective=="y"&& string.IsNullOrEmpty(source.expirationDate))
            {
                _expriationDate = 7258089599000;
            }
            else
            {
                _expriationDate = long.TryParse(source.expirationDate, out long expiration) ? expiration : 0;
            }
            if (source.isItEffective == "y" && string.IsNullOrEmpty(source.numberOfDays))
            {
                _numberOfDays = 99999;
            }
            else 
            {
                _numberOfDays = int.TryParse(source.numberOfDays, out int days) ? days : 0;
            }

            return new MysoftConfig
            {
                author = source.user,
                softwareMd5 = source.softwareMd5,
                softwareName = source.softwareName,
                softwareId = source.softwareId,
                versionNumber = source.versionNumber,
                mandatoryUpdate = source.mandatoryUpdate?.ToLower() == "y",
                numberOfVisits = int.TryParse(source.numberOfVisits, out int visits) ? visits : 0,
                miniVersion = source.miniVersion,
                timeStamp = long.TryParse(source.timeStamp, out long timestamp) ? timestamp : 0,
                networkVerificationId = source.networkVerificationId,
                isItEffective = source.isItEffective?.ToLower() == "y",
                numberOfDays = _numberOfDays,
                networkVerificationRemarks = source.networkVerificationRemarks,
                expirationDate = _expriationDate,
                notice = source.notice,
                versionInformation = source.versionInformation,
                bilibiliLink = "https://space.bilibili.com/3493128132626725"
            };
        }
        #region 异步方法

        /// <summary>
        /// 异步读取配置文件
        /// </summary>
        /// <returns>配置对象</returns>
        public async Task<AppSettingsModel> ReadConfigAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    var defaultConfig = CreateDefaultConfig();
                    await WriteConfigAsync(defaultConfig);
                    return defaultConfig;
                }

                await using FileStream fileStream = new(_filePath, FileMode.Open, FileAccess.Read);
                return await JsonSerializer.DeserializeAsync<AppSettingsModel>(fileStream, _jsonOptions)
                       ?? CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"异步读取配置文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步写入配置文件
        /// </summary>
        /// <param name="config">配置对象</param>
        public async Task WriteConfigAsync(AppSettingsModel config)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using FileStream fileStream = new(_filePath, FileMode.Create, FileAccess.Write);
                await JsonSerializer.SerializeAsync(fileStream, config, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"异步写入配置文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步更新配置文件
        /// </summary>
        /// <param name="updateAction">更新操作</param>
        public async Task UpdateConfigAsync(Action<AppSettingsModel> updateAction)
        {
            try
            {
                var config = await ReadConfigAsync();
                updateAction(config);
                await WriteConfigAsync(config);
            }
            catch (Exception ex)
            {
                throw new Exception($"异步更新配置文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步更新Mysoft配置
        /// </summary>
        /// <param name="updateAction">更新操作</param>
        public async Task UpdateMysoftConfigAsync(Action<MysoftConfig> updateAction)
        {
            await UpdateConfigAsync(config => updateAction(config.Mysoft));
        }

       
        #endregion
        #region 辅助方法
        /// <summary>
        /// 创建默认配置
        /// </summary>
        private AppSettingsModel CreateDefaultConfig()
        {
            return new AppSettingsModel
            {
                Mysoft = new MysoftConfig(),
            };
        }

        #endregion
    }
}