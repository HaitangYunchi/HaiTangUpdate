/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library.bilibili
 * 唯一标识：e8c5ca74-bff6-4c9d-9cd6-0d09f90feca6
 * 文件名：CookieData
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/30 0:21:00
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

namespace HaiTang.library.bilibili
{
    /// <summary>
    /// Cookie数据模型
    /// </summary>
    public class CookieData
    {
        public string SessData { get; set; } = string.Empty;
        public string BiliJct { get; set; } = string.Empty;
        public string DedeUserID { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public DateTime ExpireTime { get; set; } = DateTime.Now.AddMonths(1);
    }
}
