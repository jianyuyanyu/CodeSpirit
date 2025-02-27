namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// �������Ŀͻ���ѡ��
/// </summary>
public class ConfigCenterClientOptions
{
    /// <summary>
    /// �������ķ����ַ
    /// </summary>
    public string ServiceUrl { get; set; }

    /// <summary>
    /// Ӧ��ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// Ӧ����Կ
    /// </summary>
    public string AppSecret { get; set; }

    /// <summary>
    /// ��������
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// �Ƿ��Զ�ע��Ӧ��
    /// </summary>
    public bool AutoRegisterApp { get; set; } = false;

    /// <summary>
    /// Ӧ�����ƣ��������Զ�ע�ᣩ
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// ��ѯ���ø��µ�ʱ�������룩
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// �Ƿ�ʹ��SignalRʵʱ�������ñ��
    /// </summary>
    public bool UseSignalR { get; set; } = true;

    /// <summary>
    /// ���û�ȡ��ʱʱ�䣨�룩
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// �Ƿ����ñ��ػ���
    /// </summary>
    public bool EnableLocalCache { get; set; } = true;

    /// <summary>
    /// ���ػ���Ŀ¼
    /// </summary>
    public string LocalCacheDirectory { get; set; } = ".config-cache";

    /// <summary>
    /// �����ļ��������Ч�ڣ����ӣ���������ʱ�佫��Ϊ�������
    /// Ĭ��Ϊ1440���ӣ�24Сʱ��
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 1440;

    /// <summary>
    /// ��������Դ����ʱ���Ƿ���Ȼ����ʹ�û���
    /// </summary>
    public bool PreferCache { get; set; } = false;
}