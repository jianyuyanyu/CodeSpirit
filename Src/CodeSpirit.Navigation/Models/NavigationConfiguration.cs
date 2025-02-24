using System.Collections.Generic;

public class NavigationConfigItem
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Path { get; set; }
    public string Link { get; set; }
    public string Icon { get; set; }
    public int Order { get; set; }
    public string ParentPath { get; set; }
    public bool Hidden { get; set; }
    public string Permission { get; set; }
    public string Description { get; set; }
    public bool IsExternal { get; set; }
    public string Target { get; set; }
    public string ModuleName { get; set; }
    public string Route { get; set; }
    public List<NavigationConfigItem> Children { get; set; } = [];
} 