/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library
 * 唯一标识：453c1437-c5f2-4044-ab95-efb2052fdda4
 * 文件名：SaltAesEncryp
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/11/30 16:12:59
 * 版本：V1.0.0
 * 描述：基于AES加密算法和盐值的加密工具类，提供安全的字符串加密解密功能
 *       使用PBKDF2密钥派生和自动IV生成，增强安全性
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HaiTang.library
{
    /// <summary>
    /// 基于AES加密算法和盐值的加密工具类
    /// 提供使用PBKDF2密钥派生和自动IV生成的安全加密解密功能
    /// </summary>
    /// <remarks>
    /// 该类使用以下安全特性：
    /// 1. PBKDF2密钥派生函数，迭代次数10000次，使用SHA512哈希算法
    /// 2. 自动生成随机IV（初始化向量）
    /// 3. 盐值增强密码安全性
    /// 4. 256位AES密钥长度
    /// </remarks>
    public static class SaltAesEncry
    {
        /// <summary>
        /// 使用AES算法加密明文字符串
        /// </summary>
        /// <param name="plainText">要加密的明文字符串</param>
        /// <param name="password">加密密码，用于生成加密密钥</param>
        /// <param name="salt">盐值，用于增强密码安全性，防止彩虹表攻击</param>
        /// <returns>返回Base64格式的加密字符串，包含IV和加密数据</returns>
        /// <exception cref="ArgumentNullException">当plainText为null时抛出</exception>
        /// <exception cref="CryptographicException">当加密过程中出现加密错误时抛出</exception>
        /// <remarks>
        /// 加密过程：
        /// 1. 使用PBKDF2从密码和盐值派生256位AES密钥
        /// 2. 自动生成16字节的随机IV
        /// 3. 将IV写入输出流的前16字节
        /// 4. 使用AES-CBC模式加密数据
        /// 5. 返回Base64编码的完整密文（IV+加密数据）
        /// 
        /// 安全特性：
        /// - 迭代次数：10000次，平衡安全性和性能
        /// - 密钥长度：256位
        /// - 哈希算法：SHA512
        /// - 每次加密生成不同的IV，确保相同明文产生不同密文
        /// </remarks>
        /// <example>
        /// 使用示例：
        /// <code>
        /// string encrypted = SaltAesEncry.Encrypt("敏感数据", "myPassword", "saltValue");
        /// </code>
        /// </example>
        public static string Encrypt(string plainText, string password, string salt)
        {
            // 输入验证：如果明文字符串为空或null，直接返回原值
            if (string.IsNullOrEmpty(plainText)) return plainText;

            // 使用AES算法实例进行加密操作
            using (var aes = Aes.Create())
            {
                // 使用PBKDF2密钥派生函数从密码和盐值生成加密密钥
                // 增强安全性：使用高迭代次数和强哈希算法
                byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                    password: password,           // 用户提供的密码
                    salt: saltBytes,              // 盐值字节数组
                    iterations: 10000,            // 迭代次数，增加暴力破解难度
                    hashAlgorithm: HashAlgorithmName.SHA512, // 使用SHA512哈希算法
                    outputLength: 32              // 生成256位（32字节）AES密钥
                );

                // 设置AES加密密钥
                aes.Key = key;

                // 自动生成随机初始化向量(IV)
                // 重要：每次加密都生成不同的IV，防止模式分析攻击
                aes.GenerateIV();

                // 使用内存流存储加密结果
                using (var ms = new MemoryStream())
                {
                    // 首先将IV写入输出流的前16字节
                    // 解密时需要从密文开头读取IV
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    // 创建加密转换器和加密流
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        // 将明文字符串写入加密流，自动进行加密
                        sw.Write(plainText);
                    }
                    // 注意：CryptoStream会自动刷新和处置

                    // 将内存流中的加密数据转换为Base64字符串返回
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 解密使用AES算法加密的字符串
        /// </summary>
        /// <param name="cipherText">Base64格式的加密字符串，必须包含IV和加密数据</param>
        /// <param name="password">解密密码，必须与加密时使用的密码相同</param>
        /// <param name="salt">盐值，必须与加密时使用的盐值相同</param>
        /// <returns>返回解密后的明文字符串</returns>
        /// <exception cref="ArgumentNullException">当cipherText为null或空时抛出</exception>
        /// <exception cref="FormatException">当cipherText不是有效的Base64字符串时抛出</exception>
        /// <exception cref="CryptographicException">
        /// 当解密过程中出现错误时抛出，可能原因：
        /// - 密码错误
        /// - 盐值错误
        /// - 密文被篡改
        /// - 数据损坏
        /// </exception>
        /// <remarks>
        /// 解密过程：
        /// 1. 从Base64字符串解码得到完整密文字节数组
        /// 2. 使用相同的PBKDF2参数从密码和盐值派生AES密钥
        /// 3. 从密文前16字节提取IV
        /// 4. 使用密钥和IV创建解密器
        /// 5. 解密剩余的数据部分
        /// 6. 返回解密后的明文字符串
        /// </remarks>
        /// <example>
        /// 使用示例：
        /// <code>
        /// string decrypted = SaltAesEncry.Decrypt(encryptedString, "myPassword", "saltValue");
        /// </code>
        /// </example>
        public static string Decrypt(string cipherText, string password, string salt)
        {
            // 输入验证：如果密文字符串为空或null，直接返回原值
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            // 将Base64格式的密文字符串解码为字节数组
            var fullCipher = Convert.FromBase64String(cipherText);

            // 使用AES算法实例进行解密操作
            using (var aes = Aes.Create())
            {
                // 使用与加密时相同的PBKDF2参数生成密钥
                // 重要：必须使用相同的密码、盐值、迭代次数和哈希算法
                byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
                byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                    password: password,           // 必须与加密密码相同
                    salt: saltBytes,              // 必须与加密盐值相同
                    iterations: 10000,            // 必须与加密迭代次数相同
                    hashAlgorithm: HashAlgorithmName.SHA512, // 必须与加密哈希算法相同
                    outputLength: 32              // 必须与加密密钥长度相同
                );

                // 设置AES解密密钥
                aes.Key = key;

                // 从密文开头提取IV（初始化向量）
                // AES IV固定为16字节长度
                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // 创建解密转换器
                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                // 创建内存流，跳过前16字节的IV，只处理加密数据部分
                using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                // 创建解密流
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                // 使用流读取器读取解密后的文本
                using (var sr = new StreamReader(cs))
                {
                    // 读取所有解密内容并返回
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 生成密码学安全的随机盐值
        /// </summary>
        /// <param name="length">盐值的字节长度，默认为64字节</param>
        /// <returns>返回Base64编码的随机盐值字符串</returns>
        /// <exception cref="ArgumentOutOfRangeException">当length小于等于0时抛出</exception>
        /// <remarks>
        /// 盐值的作用：
        /// 1. 防止彩虹表攻击：即使两个用户使用相同密码，由于盐值不同，也会生成不同的密钥
        /// 2. 增加密码复杂性：盐值增加了密钥的随机性和复杂性
        /// 3. 防止预计算攻击：攻击者无法预先计算常用密码的哈希值
        /// 
        /// 安全建议：
        /// - 每个用户或每个加密操作应使用不同的盐值
        /// - 盐值应足够长（推荐至少64字节）
        /// - 盐值应随机生成，不可预测
        /// - 盐值可以明文存储，但必须与密文分开保存
        /// </remarks>
        /// <example>
        /// 使用示例：
        /// <code>
        /// string salt = SaltAesEncry.GenerateSalt(); // 生成64字节盐值
        /// string customSalt = SaltAesEncry.GenerateSalt(32); // 生成32字节盐值
        /// </code>
        /// </example>
        public static string GenerateSalt(int length = 64)
        {
            // 验证输入参数
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "盐值长度必须大于0");

            // 创建指定长度的随机字节数组
            var randomBytes = new byte[length];

            // 使用密码学安全的随机数生成器
            using (var rng = RandomNumberGenerator.Create())
            {
                // 填充随机字节
                rng.GetBytes(randomBytes);
                // 将随机字节数组转换为Base64字符串返回
                return Convert.ToBase64String(randomBytes);
            }
        }
    }
}