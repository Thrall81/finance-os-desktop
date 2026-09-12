using System;
using FinanceOS.App;
using UnityEngine;
using UnityEngine.UIElements;

namespace FinanceOS.UI
{
    /// <summary>
    /// The scene's entry point: builds the AppContainer and renders the first screen. Thin by
    /// design — everything it does is one call into App or UI, per docs/02-Architecture.md §4.
    /// Only shows the dashboard for now; the no-account/onboarding state
    /// (docs/07-Interface.md §4) is not wired up yet.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppBootstrap : MonoBehaviour
    {
        public static AppContainer? Container { get; private set; }

        private DashboardController? _dashboardController;

        private void Awake()
        {
            Container = new AppContainer();
            Container.Categories.SeedDefaultCategoriesIfEmpty();

            var root = GetComponent<UIDocument>().rootVisualElement;
            _dashboardController = new DashboardController(root);
            RefreshDashboard();
        }

        private void RefreshDashboard()
        {
            if (Container is null || _dashboardController is null)
            {
                return;
            }

            var viewModel = DashboardViewModelBuilder.Build(Container, DateTime.Now);
            if (viewModel is not null)
            {
                _dashboardController.Render(viewModel);
            }
        }

        private void OnApplicationQuit()
        {
            Container?.Dispose();
            Container = null;
        }
    }
}
