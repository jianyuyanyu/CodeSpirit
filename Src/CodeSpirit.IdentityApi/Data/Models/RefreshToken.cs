using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Shared.Entities;

namespace CodeSpirit.IdentityApi.Data.Models
{
    /// <summary>
    /// 刷新令牌实体，用于实现JWT令牌的刷新功能
    /// </summary>
    [Table("RefreshTokens")]
    public class RefreshToken : EntityBase<long>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public long UserId { get; set; }

        /// <summary>
        /// 关联的用户
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        /// <summary>
        /// 刷新令牌字符串
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string Token { get; set; }

        /// <summary>
        /// 关联的JWT令牌ID
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string JwtId { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// 是否已被撤销
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// 创建新的刷新令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="token">刷新令牌字符串</param>
        /// <param name="jwtId">关联的JWT令牌ID</param>
        /// <param name="expiryTime">过期时间</param>
        /// <returns>创建的刷新令牌实体</returns>
        public static RefreshToken Create(long userId, string token, string jwtId, DateTime expiryTime)
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = token,
                JwtId = jwtId,
                IsUsed = false,
                IsRevoked = false,
                CreatedTime = DateTime.UtcNow,
                ExpiryTime = expiryTime
            };
        }
    }
} 