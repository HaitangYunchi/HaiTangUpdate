/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library.bilibili
 * 唯一标识：c7d269ac-da42-46f9-80ea-9d36276eb719
 * 文件名：UserInfo
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/30 0:19:51
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
using System.Text.Json.Serialization;

namespace HaiTang.library.bilibili
{
    /// <summary>
    /// 用户信息模型
    /// </summary>
    public class BlibiliUserInfo
    {
        [JsonPropertyName("mid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Mid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sex")]
        public string Sex { get; set; } = string.Empty;

        [JsonPropertyName("face")]
        public string Face { get; set; } = string.Empty;

        [JsonPropertyName("sign")]
        public string Sign { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("following")]
        public long Following { get; set; }

        [JsonPropertyName("follower")]
        public long Follower { get; set; }

        [JsonPropertyName("birthday")]
        [JsonConverter(typeof(BirthdayConverter))]
        public string Birthday { get; set; } = string.Empty;

        [JsonPropertyName("coins")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Coins { get; set; }

        [JsonPropertyName("vip")]
        public VipInfo Vip { get; set; } = new VipInfo();
    }

    /// <summary>
    /// 生日字段转换器 - 处理多种可能的生日格式
    /// </summary>
    public class BirthdayConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        return reader.GetString() ?? string.Empty;
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out int intValue))
                        {
                            // 如果生日是数字，可能是时间戳或特殊值
                            return intValue == 0 ? "未设置" : intValue.ToString();
                        }
                        if (reader.TryGetInt64(out long longValue))
                        {
                            return longValue == 0 ? "未设置" : longValue.ToString();
                        }
                        return reader.GetDecimal().ToString();
                    case JsonTokenType.Null:
                        return "未设置";
                    default:
                        return string.Empty;
                }
            }
            catch
            {
                return "未知";
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    /// <summary>
    /// VIP信息模型
    /// </summary>
    public class VipInfo
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("label")]
        public VipLabel Label { get; set; } = new VipLabel();

        [JsonIgnore]
        public bool VipStatus => Status == 1;

        [JsonIgnore]
        public string VipType => Type == 2 ? "年度大会员" : Type == 1 ? "月度大会员" : "非大会员";
    }

    /// <summary>
    /// VIP标签模型
    /// </summary>
    public class VipLabel
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户统计信息模型
    /// </summary>
    public class UserStat
    {
        [JsonPropertyName("following")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Following { get; set; }

        [JsonPropertyName("whisper")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Whisper { get; set; }

        [JsonPropertyName("black")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Black { get; set; }

        [JsonPropertyName("follower")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Follower { get; set; }
    }

    /// <summary>
    /// UP主统计信息模型
    /// </summary>
    public class UpStat
    {
        [JsonPropertyName("archive")]
        public ArchiveStat Archive { get; set; } = new ArchiveStat();

        [JsonPropertyName("article")]
        public ArticleStat Article { get; set; } = new ArticleStat();

        [JsonPropertyName("likes")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Likes { get; set; }

        [JsonIgnore]
        public long ArchiveView => Archive.View;

        [JsonIgnore]
        public long ArticleView => Article.View;
    }

    /// <summary>
    /// 视频统计模型
    /// </summary>
    public class ArchiveStat
    {
        [JsonPropertyName("view")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long View { get; set; }
    }

    /// <summary>
    /// 专栏统计模型
    /// </summary>
    public class ArticleStat
    {
        [JsonPropertyName("view")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long View { get; set; }
    }
}
