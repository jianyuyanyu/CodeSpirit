using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.OrderApi.Data.Models
{
    /// <summary>
    /// 订单信息
    /// </summary>
    public class Order : IIsActive, IFullEntityEvent
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        [Required]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// 下单时间
        /// </summary>
        public DateTime OrderTime { get; set; }

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PayTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(500)]
        public string Remarks { get; set; }

        // 实现接口的属性
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }
        public long? DeleterUserId { get; set; }
        public long? CreatorUserId { get; set; }
        public DateTime CreationTime { get; set; }
        public long? LastModifierUserId { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }

    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// 待支付
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已支付
        /// </summary>
        Paid = 1,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 2,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 已退款
        /// </summary>
        Refunded = 4
    }
} 