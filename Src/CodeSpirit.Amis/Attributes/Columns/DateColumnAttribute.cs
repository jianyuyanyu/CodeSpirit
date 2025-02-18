using System;

namespace CodeSpirit.Amis.Attributes.Columns
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateColumnAttribute : Attribute
    {
        public string Format { get; set; }
        public string InputFormat { get; set; }
        public string Placeholder { get; set; }
        public int TimeZone { get; set; }

        /// <summary>
        /// �Ƿ���ʾ��Ե�ǰ��ʱ������������: 11 Сʱǰ��3 ��ǰ��1 ��ǰ�ȣ�fromNow Ϊ true ʱ��format ����Ч��
        /// </summary>
        public bool FromNow { get; set; }
    }
}