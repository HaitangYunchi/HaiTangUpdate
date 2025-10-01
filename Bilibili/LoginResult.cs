/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library.bilibili
 * 唯一标识：f1072ac1-6c18-47dd-bcc0-5e5d523c7837
 * 文件名：LoginResult
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/30 0:20:31
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
    /// 扫码登录结果模型
    /// </summary>
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string SessData { get; set; } = string.Empty;
        public string BiliJct { get; set; } = string.Empty;
        public string DedeUserID { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }
}
