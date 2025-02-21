using CodeSpirit.Core.Attributes;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Navigation.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSpirit.Navigation
{
    public partial class NavigationService
	{
		private async Task UpdateModuleNavigationCache(string moduleName)
		{
			var cacheKey = $"{CACHE_KEY_PREFIX}{moduleName}";
			var moduleNavigation = BuildModuleNavigationTree(moduleName);
			var existingNavigation = await _cache.GetAsync<List<NavigationNode>>(cacheKey);

			if (existingNavigation != null)
			{
				MergeNavigationNodes(existingNavigation[0], moduleNavigation[0]);
				moduleNavigation = existingNavigation;
			}

			await _cache.SetAsync(cacheKey, moduleNavigation, _cacheOptions);
		}

		private List<NavigationNode> BuildModuleNavigationTree(string moduleName)
		{
			var controllers = _actionProvider.ActionDescriptors.Items
				.OfType<ControllerActionDescriptor>()
				.Where(x => x.ControllerTypeInfo.GetCustomAttribute<ModuleAttribute>()?.Name == moduleName)
				.GroupBy(x => x.ControllerTypeInfo);

			var moduleNode = new NavigationNode(moduleName, moduleName, string.Empty);

			foreach (var controller in controllers)
			{
				var navAttr = controller.Key.GetCustomAttribute<NavigationAttribute>();
				if (navAttr != null && !navAttr.Hidden)
				{
					var controllerNode = CreateNavigationNode(navAttr, controller.Key.Name);
					moduleNode.Children.Add(controllerNode);

					foreach (var action in controller)
					{
						var actionNavAttr = action.MethodInfo.GetCustomAttribute<NavigationAttribute>();
						if (actionNavAttr != null && !actionNavAttr.Hidden)
						{
							var actionNode = CreateNavigationNode(actionNavAttr, action.ActionName);
							controllerNode.Children.Add(actionNode);
						}
					}
				}
			}

			return [moduleNode];
		}

		private NavigationNode CreateNavigationNode(NavigationAttribute attr, string defaultName)
		{
			return new NavigationNode(defaultName, attr.Title ?? defaultName, attr.Path)
			{
				Icon = attr.Icon,
				Order = attr.Order,
				ParentPath = attr.ParentPath,
				Hidden = attr.Hidden,
				Permission = attr.Permission,
				Description = attr.Description,
				IsExternal = attr.IsExternal,
				Target = attr.Target
			};
		}

		private void MergeNavigationNodes(NavigationNode existing, NavigationNode current)
		{
			existing.Title = current.Title;
			existing.Path = current.Path;
			existing.Icon = current.Icon;
			existing.Order = current.Order;
			existing.ParentPath = current.ParentPath;
			existing.Hidden = current.Hidden;
			existing.Permission = current.Permission;
			existing.Description = current.Description;
			existing.IsExternal = current.IsExternal;
			existing.Target = current.Target;

			foreach (var currentChild in current.Children)
			{
				var existingChild = existing.Children.FirstOrDefault(c => c.Name == currentChild.Name);
				if (existingChild != null)
				{
					MergeNavigationNodes(existingChild, currentChild);
				}
				else
				{
					existing.Children.Add(currentChild);
				}
			}
		}
	}
}
