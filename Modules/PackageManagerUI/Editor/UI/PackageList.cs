// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        internal IEnumerable<PackageItem> packageItems => packageGroups.SelectMany(group => group.packageItems);
        private IEnumerable<PackageGroup> packageGroups => itemsList.Children().Cast<PackageGroup>();

        public PackageList()
        {
            var root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            viewDataKey = "package-list-key";
            scrollView.viewDataKey = "package-list-scrollview-key";

            loginButton.clickable.clicked += OnLoginClicked;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            focusable = true;
        }

        public void OnEnable()
        {
            PackageDatabase.instance.onPackageProgressUpdate += OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart += OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange += OnVisualStateChange;
            PageManager.instance.onListRebuild += OnListRebuild;
            PageManager.instance.onListUpdate += OnListUpdate;

            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;

            // manually build the items on initialization to refresh the UI
            OnListRebuild(PageManager.instance.GetCurrentPage());
        }

        public void OnDisable()
        {
            PackageDatabase.instance.onPackageProgressUpdate -= OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange -= OnVisualStateChange;
            PageManager.instance.onListRebuild -= OnListRebuild;
            PageManager.instance.onListUpdate -= OnListUpdate;

            ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private PackageItem GetPackageItem(string packageUniqueId)
        {
            return string.IsNullOrEmpty(packageUniqueId) ? null : m_PackageItemsLookup.Get(packageUniqueId);
        }

        private ISelectableItem GetSelectedItem()
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            if (!packageItem.visualState.expanded)
                return packageItem;

            return packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                RefreshList(false);
        }

        private void ShowPackages(bool updateScrollPosition)
        {
            UIUtils.SetElementDisplay(scrollView, true);
            UIUtils.SetElementDisplay(emptyArea, false);

            var page = PageManager.instance.GetCurrentPage();
            var selectedVersion = page.GetSelectedVersion();
            var selectedVisualState = selectedVersion != null ? page.GetVisualState(selectedVersion.packageUniqueId) : null;
            if (selectedVisualState?.visible != true)
            {
                var firstVisible = page.visualStates.FirstOrDefault(v => v.visible);
                if (firstVisible != null)
                {
                    IPackage package;
                    IPackageVersion version;
                    PackageDatabase.instance.GetPackageAndVersion(firstVisible.packageUniqueId, firstVisible.selectedVersionId, out package, out version);
                    PageManager.instance.SetSelected(package, version);
                }
                else
                    PageManager.instance.ClearSelection();
            }

            if (updateScrollPosition)
                ScrollIfNeeded();
        }

        private void HidePackagesShowLogin()
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, false);
            UIUtils.SetElementDisplay(loginContainer, true);

            PageManager.instance.ClearSelection();
        }

        private void HidePackagesShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone)
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, true);
            UIUtils.SetElementDisplay(loginContainer, false);

            if (isRefreshInProgress)
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("Fetching packages...");
                else
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("Refreshing packages...");
            }
            else if (string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText))
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = string.Empty;
                else
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("There are no packages.");
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = PackageFiltering.instance.currentSearchText;
                if (searchText?.Length > maxSearchTextToDisplay)
                    searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                noPackagesLabel.text = string.Format(ApplicationUtil.instance.GetTranslationForText("No results for \"{0}\""), searchText);
            }

            PageManager.instance.ClearSelection();
        }

        private void RefreshList(bool updateScrollPosition)
        {
            var isAssetStore = PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;
            var isLoginInfoReady = ApplicationUtil.instance.isUserInfoReady;
            var isLoggedIn = ApplicationUtil.instance.isUserLoggedIn;

            if (isAssetStore && isLoginInfoReady && !isLoggedIn)
            {
                HidePackagesShowLogin();
                return;
            }

            var page = PageManager.instance.GetCurrentPage();
            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = PageManager.instance.IsInitialFetchingDone();

            if (isListEmpty || !isInitialFetchingDone)
            {
                HidePackagesShowMessage(PageManager.instance.IsRefreshInProgress(), isInitialFetchingDone);
                return;
            }

            ShowPackages(updateScrollPosition);
        }

        private void OnLoginClicked()
        {
            ApplicationUtil.instance.ShowLogin();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            if (panel != null)
                panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (panel != null)
                panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnPackageProgressUpdate(IPackage package)
        {
            GetPackageItem(package?.uniqueId)?.RefreshState();
        }

        private void OnRefreshOperationStart()
        {
            RefreshList(false);
        }

        private void OnRefreshOperationFinish()
        {
            RefreshList(false);
        }

        internal void OnFocus()
        {
            ScrollIfNeeded();
        }

        private void ScrollIfNeeded(ScrollView container = null, VisualElement target = null)
        {
            container = container ?? scrollView;
            target = target ?? GetSelectedItem()?.element;

            if (container == null || target == null)
                return;

            if (float.IsNaN(target.layout.height))
            {
                EditorApplication.delayCall += () => ScrollIfNeeded(container, target);
                return;
            }

            var scrollViews = UIUtils.GetParentsOfType<ScrollView>(target);
            foreach (var scrollview in scrollViews)
                UIUtils.ScrollIfNeeded(scrollview, target);
        }

        private void SetSelectedItemExpanded(bool value)
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            GetPackageItem(selectedVersion?.packageUniqueId)?.SetExpanded(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(scrollView))
                return;

            if (evt.keyCode == KeyCode.RightArrow)
            {
                SetSelectedItemExpanded(true);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.LeftArrow)
            {
                SetSelectedItemExpanded(false);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (SelectNext(true))
                    evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (SelectNext(false))
                    evt.StopPropagation();
            }
        }

        private void AddOrUpdatePackageItem(VisualState state, IPackage package = null)
        {
            package = package ?? PackageDatabase.instance.GetPackage(state?.packageUniqueId);
            if (package == null)
                return;

            var item = GetPackageItem(state.packageUniqueId);
            if (item != null)
            {
                item.SetPackage(package);
                item.UpdateVisualState(state);
            }
            else
            {
                var group = GetOrCreateGroup(state.groupName);
                item = group.AddPackageItem(package, state);
                m_PackageItemsLookup[package.uniqueId] = item;
            }
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            var group = packageGroups.FirstOrDefault(g => g.name == groupName);
            if (group != null)
                return group;

            var hidden = string.IsNullOrEmpty(groupName);
            var expanded = PageManager.instance.IsGroupExpanded(groupName);
            group = new PackageGroup(groupName, expanded, hidden);
            if (!hidden)
            {
                group.onGroupToggle += value =>
                {
                    if (value && group.Contains(GetSelectedItem()))
                        EditorApplication.delayCall += () => ScrollIfNeeded();
                };
            }
            itemsList.Add(group);
            return group;
        }

        private void RemovePackageItem(string packageUniqueId)
        {
            var item = GetPackageItem(packageUniqueId);
            if (item != null)
            {
                item.packageGroup.Remove(item);
                m_PackageItemsLookup.Remove(packageUniqueId);
            }
        }

        private void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            RefreshList(true);
        }

        private void OnListRebuild(IPage page)
        {
            itemsList.Clear();
            m_PackageItemsLookup.Clear();

            foreach (var visualState in page.visualStates)
                AddOrUpdatePackageItem(visualState);

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            RefreshList(true);
        }

        private void OnListUpdate(IPage page, IEnumerable<IPackage> addedOrUpdated, IEnumerable<IPackage> removed, bool reorder)
        {
            addedOrUpdated = addedOrUpdated ?? Enumerable.Empty<IPackage>();
            removed = removed ?? Enumerable.Empty<IPackage>();

            var numItems = m_PackageItemsLookup.Count;
            foreach (var package in removed)
                RemovePackageItem(package?.uniqueId);

            var itemsRemoved = numItems != m_PackageItemsLookup.Count;
            numItems = m_PackageItemsLookup.Count;

            foreach (var package in addedOrUpdated)
            {
                var visualState = page.GetVisualState(package.uniqueId) ?? new VisualState(package.uniqueId, string.Empty);
                AddOrUpdatePackageItem(visualState, package);
            }
            var itemsAdded = numItems != m_PackageItemsLookup.Count;

            if (reorder)
            {
                // re-order if there are any added or updated items
                foreach (var group in packageGroups)
                    group.ClearPackageItems();

                foreach (var state in page.visualStates)
                {
                    var packageItem = GetPackageItem(state.packageUniqueId);
                    packageItem.packageGroup.AddPackageItem(packageItem);
                }

                m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);
            }

            if (itemsRemoved || itemsAdded)
            {
                foreach (var group in packageGroups)
                    group.RefreshHeaderVisibility();

                RefreshList(true);
            }
        }

        internal bool SelectNext(bool reverseOrder)
        {
            var nextElement = FindNextVisibleSelectableItem(reverseOrder);
            if (nextElement != null)
            {
                PageManager.instance.SetSelected(nextElement.package, nextElement.targetVersion);
                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
                return true;
            }
            return false;
        }

        private ISelectableItem FindNextVisibleSelectableItem(bool reverseOrder)
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            // First we try to look for the next visible options within all the versions of the current package when the list is expanded
            if (packageItem.visualState.expanded)
            {
                // When the current selection is in the version list, we look in the list first
                var versionItem = packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
                var nextVersionItem = UIUtils.FindNextSibling(versionItem, reverseOrder, UIUtils.IsElementVisible) as PackageVersionItem;
                if (nextVersionItem != null)
                    return nextVersionItem;
            }

            var nextPackageItem = FindNextVisiblePackageItem(packageItem, reverseOrder);
            if (nextPackageItem == null)
                return null;

            if (nextPackageItem.visualState.expanded)
            {
                var nextVersionItem = reverseOrder ? nextPackageItem.versionItems.LastOrDefault() : nextPackageItem.versionItems.FirstOrDefault();
                if (nextVersionItem != null)
                    return nextVersionItem;
            }
            return nextPackageItem;
        }

        private static PackageItem FindNextVisiblePackageItem(PackageItem packageItem, bool reverseOrder)
        {
            PackageItem nextVisibleItem = null;
            if (packageItem.packageGroup.expanded)
                nextVisibleItem = UIUtils.FindNextSibling(packageItem, reverseOrder, UIUtils.IsElementVisible) as PackageItem;

            if (nextVisibleItem == null)
            {
                Func<VisualElement, bool> expandedNonEmptyGroup = (element) =>
                {
                    var group = element as PackageGroup;
                    return group.expanded && group.packageItems.Any(p => UIUtils.IsElementVisible(p));
                };
                var nextGroup = UIUtils.FindNextSibling(packageItem.packageGroup, reverseOrder, expandedNonEmptyGroup) as PackageGroup;
                if (nextGroup != null)
                    nextVisibleItem = reverseOrder ? nextGroup.packageItems.LastOrDefault(p => UIUtils.IsElementVisible(p))
                        : nextGroup.packageItems.FirstOrDefault(p => UIUtils.IsElementVisible(p));
            }
            return nextVisibleItem;
        }

        internal int CalculateNumberOfPackagesToDisplay()
        {
            return ApplicationUtil.instance.CalculateNumberOfElementsInsideContainerToDisplay(this, 38);
        }

        private VisualElementCache cache { get; set; }

        private ScrollView scrollView { get { return cache.Get<ScrollView>("scrollView"); } }
        private VisualElement itemsList { get { return cache.Get<VisualElement>("itemsList"); } }
        private VisualElement emptyArea { get { return cache.Get<VisualElement>("emptyArea"); } }
        private Label noPackagesLabel { get { return cache.Get<Label>("noPackagesLabel"); } }
        private VisualElement loginContainer { get { return cache.Get<VisualElement>("loginContainer"); } }
        private Button loginButton { get { return cache.Get<Button>("loginButton"); } }
    }
}
