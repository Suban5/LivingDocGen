using System.Linq;
using LivingDocGen.Generator.Models;

namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Generates JavaScript for HTML living documentation.
/// Includes performance optimizations, lazy loading, and interactive features.
/// </summary>
public class JavaScriptGenerator : IJavaScriptGenerator
{
    /// <inheritdoc/>
    public string Generate(LivingDocumentation documentation, bool useLazyRendering = false)
    {
        var featureCount = documentation.Features.Count;
        var hasLargeReport = featureCount > 100;
        
        return @"
    <script>
        // Performance optimizations for large reports (" + featureCount + @" features)
        const PERF_LARGE_REPORT = " + hasLargeReport.ToString().ToLower() + @";
        const FEATURE_COUNT = " + featureCount + @";
        const USE_LAZY_RENDERING = " + useLazyRendering.ToString().ToLower() + @";
        
        // Feature Navigation State
        let currentFeatureId = 'feature-0';
        
        // ============================================
        // EARLY FUNCTION DECLARATIONS (must be available for onclick handlers)
        // ============================================
        
        // Pre-declare these functions so they're available for inline onclick handlers
        // Full implementations are below, but these stubs prevent 'undefined' errors
        
        // Select Feature - stub that will be enhanced later
        function selectFeature(featureId) {
            // Show loader for large reports
            if (FEATURE_COUNT > 100 && typeof showLoader === 'function') {
                showLoader('Loading feature...', 30);
            }
            
            // Immediate visual feedback - hide all features
            requestAnimationFrame(() => {
                document.querySelectorAll('.feature[data-feature-id]').forEach(feature => {
                    feature.classList.add('feature-hidden');
                });
            });
            
            // Defer heavy operations to idle time
            const idleCallback = typeof requestIdleCallback !== 'undefined' ? requestIdleCallback : setTimeout;
            idleCallback(() => {
                // Show selected feature
                let selectedFeature = document.getElementById(featureId);
                if (selectedFeature) {
                    // Render lazy-loaded feature content if needed
                    if (USE_LAZY_RENDERING && selectedFeature.hasAttribute('data-lazy') && typeof renderFeatureContent === 'function') {
                        renderFeatureContent(selectedFeature);
                        // Get the element again after rendering (it was replaced)
                        selectedFeature = document.getElementById(featureId);
                    }
                    
                    // Show the feature with smooth animation
                    requestAnimationFrame(() => {
                        if (selectedFeature) {
                            selectedFeature.classList.remove('feature-hidden');
                            currentFeatureId = featureId;
                        }
                        
                        // Update active state in sidebar
                        document.querySelectorAll('.feature-item').forEach(item => {
                            item.classList.remove('active');
                        });
                        const activeItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
                        if (activeItem) {
                            activeItem.classList.add('active');
                        }
                        
                        // Scroll to top of content
                        const mainContent = document.getElementById('main-content');
                        if (mainContent) {
                            mainContent.scrollTop = 0;
                        }
                        
                        // Save last viewed feature
                        localStorage.setItem('bdd-last-feature', featureId);
                        
                        // Hide loader
                        if (typeof hideLoader === 'function') hideLoader();
                    });
                } else {
                    if (typeof hideLoader === 'function') hideLoader();
                }
            }, { timeout: 50 });
        }
        
        // Toggle Folder - Early declaration
        function toggleFolder(folderId) {
            const folderContent = document.getElementById(folderId);
            const folder = folderContent?.closest('.folder');
            if (folder) {
                const isCollapsed = folder.classList.toggle('collapsed');
                folder.setAttribute('aria-expanded', !isCollapsed);
            }
        }
        
        // Handle Feature Keydown - Early declaration
        function handleFeatureKeydown(event, featureId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                selectFeature(featureId);
            }
        }
        
        // Handle Folder Keydown - Early declaration
        function handleFolderKeydown(event, folderId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                toggleFolder(folderId);
            }
        }
        
        // Performance: Use event delegation for toggle operations in large reports
        if (PERF_LARGE_REPORT) {
            console.log('⚡ Performance mode enabled for ' + FEATURE_COUNT + ' features');
        }
        
        if (USE_LAZY_RENDERING) {
            console.log('⚡ Lazy rendering enabled - content loads on scroll');
        }
        
        // ============================================
        // PHASE 1: ADAPTIVE HEADER & STATISTICS
        // ============================================
        
        // Adaptive Header: Shrink on scroll, hide when scrolling down (past threshold)
        // Shows controls bar at top when header is hidden for easy access to filters
        let lastScrollTop = 0;
        const header = document.querySelector('header');
        const scrollThreshold = 80;
        const hideThreshold = 250; // Hide header when scrolled past this point
        let headerState = 'visible'; // 'visible', 'shrunk', 'hidden'
        let accumulatedDelta = 0; // Accumulate scroll delta for smoother transitions
        let headerUpdateTimer = null;
        
        function updateHeaderState(newState) {
            if (headerState === newState) return;
            
            // Clear any pending updates
            if (headerUpdateTimer) {
                clearTimeout(headerUpdateTimer);
                headerUpdateTimer = null;
            }
            
            // Debounce state changes to prevent flickering
            headerUpdateTimer = setTimeout(() => {
                header.classList.remove('visible', 'shrunk', 'hidden');
                header.classList.add(newState);
                
                // Also update aria for accessibility
                if (newState === 'hidden') {
                    header.setAttribute('aria-hidden', 'true');
                } else {
                    header.removeAttribute('aria-hidden');
                }
                
                headerState = newState;
                accumulatedDelta = 0; // Reset accumulator after state change
            }, 50); // Small delay to batch rapid changes
        }
        
        function handleHeaderScroll() {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const scrollDelta = scrollTop - lastScrollTop;
            
            // Accumulate scroll delta for smoother direction detection
            accumulatedDelta += scrollDelta;
            
            // When at the very top, always show full header
            if (scrollTop <= scrollThreshold) {
                updateHeaderState('visible');
                accumulatedDelta = 0;
            }
            // When scrolled a bit but not too far, show shrunk header
            else if (scrollTop > scrollThreshold && scrollTop <= hideThreshold) {
                updateHeaderState('shrunk');
                accumulatedDelta = 0;
            }
            // When scrolled past hide threshold - use accumulated delta for stable detection
            else if (scrollTop > hideThreshold) {
                // Need significant accumulated scroll (100px) to trigger state change
                // This prevents flickering from small scroll variations
                if (accumulatedDelta > 100) {
                    // Scrolling down significantly - hide header
                    updateHeaderState('hidden');
                } else if (accumulatedDelta < -150) {
                    // Scrolling up significantly - show shrunk header
                    updateHeaderState('shrunk');
                }
                // Small movements don't change state (prevents flickering)
            }
            
            lastScrollTop = Math.max(0, scrollTop);
        }
        
        // Throttle scroll events for performance
        // OPTIMIZED: Added { passive: true } for better scroll performance
        // FIX: Added scrolling class management to disable CSS transitions during scroll
        let ticking = false;
        let scrollEndTimer = null;
        const sidebar = document.getElementById('sidebar');
        
        window.addEventListener('scroll', function() {
            // Add scrolling class to disable CSS transitions during scroll (prevents flickering)
            if (sidebar) {
                sidebar.classList.add('scrolling');
            }
            if (header) {
                header.classList.add('scrolling');
            }
            
            // Clear existing timer
            if (scrollEndTimer) clearTimeout(scrollEndTimer);
            
            // Remove scrolling class after scroll ends (200ms debounce)
            scrollEndTimer = setTimeout(() => {
                if (sidebar) sidebar.classList.remove('scrolling');
                if (header) header.classList.remove('scrolling');
                accumulatedDelta = 0; // Reset accumulated delta when scroll ends
            }, 200);
            
            if (!ticking) {
                window.requestAnimationFrame(function() {
                    handleHeaderScroll();
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
        
        // Statistics Toggle Function
        function toggleStats() {
            const statsPanel = document.getElementById('stats');
            const statsToggle = document.getElementById('stats-toggle');
            const isCollapsed = statsPanel.classList.contains('collapsed');
            
            if (isCollapsed) {
                statsPanel.classList.remove('collapsed');
                statsToggle.classList.remove('collapsed');
                statsToggle.setAttribute('aria-expanded', 'true');
                localStorage.setItem('statsExpanded', 'true');
            } else {
                statsPanel.classList.add('collapsed');
                statsToggle.classList.add('collapsed');
                statsToggle.setAttribute('aria-expanded', 'false');
                localStorage.setItem('statsExpanded', 'false');
            }
        }
        
        // Restore stats state from localStorage
        document.addEventListener('DOMContentLoaded', function() {
            const statsExpanded = localStorage.getItem('statsExpanded');
            // Default to collapsed after first visit (unless explicitly set to true)
            if (statsExpanded === 'false') {
                const statsPanel = document.getElementById('stats');
                const statsToggle = document.getElementById('stats-toggle');
                if (statsPanel && statsToggle) {
                    statsPanel.classList.add('collapsed');
                    statsToggle.classList.add('collapsed');
                    statsToggle.setAttribute('aria-expanded', 'false');
                }
            }
        });
        
        // ============================================
        // SMART PROGRESSIVE DISCLOSURE
        // ============================================
        
        // Phase 3: Loading Spinner Utilities for UX feedback
        let loaderTimeout;
        
        function showLoader(message = 'Loading...', delay = 100) {
            // Don't show spinner for quick operations (<100ms)
            loaderTimeout = setTimeout(() => {
                const loader = document.getElementById('global-loader');
                if (loader) {
                    const text = loader.querySelector('.loader-text');
                    if (text) text.textContent = message;
                    loader.classList.add('active');
                    
                    // Accessibility announcement
                    announceToScreenReader(message);
                }
            }, delay);
        }
        
        function hideLoader() {
            clearTimeout(loaderTimeout); // Cancel if operation was fast
            const loader = document.getElementById('global-loader');
            if (loader) {
                loader.classList.remove('active');
            }
        }
        
        // Auto-expand failures and search hits
        function autoExpandFeature(featureId, reason) {
            const feature = document.getElementById(featureId);
            if (!feature) return;
            
            // Ensure feature is visible
            if (feature.classList.contains('feature-hidden')) {
                feature.classList.remove('feature-hidden');
            }
            
            // Auto-expand failed scenarios
            if (reason === 'failure') {
                const failedScenarios = feature.querySelectorAll('.scenario-header.failed');
                failedScenarios.forEach(header => {
                    const body = header.nextElementSibling;
                    if (body && !body.classList.contains('expanded')) {
                        body.classList.add('expanded');
                    }
                });
            }
            
            // Scroll feature into view
            feature.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
        
        // Auto-compact view for large datasets
        if (FEATURE_COUNT > 100) {
            console.log('📊 Large dataset detected - using compact view');
            // Compact mode is handled by lazy rendering
        }
        
        // ============================================
        // PERFORMANCE: UNIFIED EVENT DELEGATION
        // ============================================
        // Single event listener handles ALL toggle operations
        // Critical for 200+ features with 500+ scenarios
        
        document.addEventListener('click', function(e) {
            // Scenario toggle - most common operation (Phase 3: Enhanced with requestIdleCallback)
            const scenarioHeader = e.target.closest('.scenario-header');
            if (scenarioHeader && scenarioHeader.hasAttribute('data-toggle-scenario')) {
                e.preventDefault();
                const scenarioBody = scenarioHeader.nextElementSibling;
                if (scenarioBody && scenarioBody.classList.contains('scenario-body')) {
                    const isExpanding = !scenarioBody.classList.contains('expanded');
                    
                    if (isExpanding) {
                        // Immediate visual feedback
                        requestAnimationFrame(() => {
                            scenarioBody.style.willChange = 'max-height, opacity';
                        });
                        
                        // Defer heavy DOM work
                        requestIdleCallback(() => {
                            requestAnimationFrame(() => {
                                scenarioBody.classList.add('expanded');
                                setTimeout(() => {
                                    scenarioBody.style.willChange = 'auto';
                                }, 300);
                            });
                        }, { timeout: 50 });
                    } else {
                        // Collapse immediately (fast operation)
                        requestAnimationFrame(() => {
                            scenarioBody.classList.remove('expanded');
                        });
                    }
                }
                return;
            }
            
            // Background toggle
            const backgroundHeader = e.target.closest('.background-header');
            if (backgroundHeader) {
                e.preventDefault();
                const body = backgroundHeader.nextElementSibling;
                const toggleIcon = backgroundHeader.querySelector('.toggle-icon');
                if (body) {
                    requestAnimationFrame(() => {
                        body.classList.toggle('expanded');
                        if (toggleIcon) {
                            if (body.classList.contains('expanded')) {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            }
                        }
                    });
                }
                return;
            }
            
            // Rule toggle
            const ruleHeader = e.target.closest('.rule-header');
            if (ruleHeader) {
                e.preventDefault();
                const body = ruleHeader.nextElementSibling;
                const toggleIcon = ruleHeader.querySelector('.toggle-icon');
                if (body) {
                    requestAnimationFrame(() => {
                        body.classList.toggle('expanded');
                        if (toggleIcon) {
                            if (body.classList.contains('expanded')) {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            }
                        }
                    });
                }
                return;
            }
            
            // Data table toggle (thead click)
            const tableHeader = e.target.closest('thead.data-table-header, thead.examples-table-header');
            if (tableHeader) {
                e.preventDefault();
                const tbody = tableHeader.parentElement.querySelector('tbody');
                if (tbody) {
                    requestAnimationFrame(() => {
                        tbody.classList.toggle('collapsed');
                        tableHeader.classList.toggle('collapsed');
                    });
                }
                return;
            }
            
            // DocString toggle
            const docStringHeader = e.target.closest('.doc-string-header');
            if (docStringHeader) {
                e.preventDefault();
                const content = docStringHeader.nextElementSibling;
                if (content) {
                    requestAnimationFrame(() => {
                        docStringHeader.classList.toggle('collapsed');
                        content.classList.toggle('collapsed');
                    });
                }
                return;
            }
            
            // Examples section toggle
            const examplesHeader = e.target.closest('.examples-header');
            if (examplesHeader) {
                e.preventDefault();
                const content = examplesHeader.nextElementSibling;
                const toggleIcon = examplesHeader.querySelector('.toggle-icon');
                if (content) {
                    requestAnimationFrame(() => {
                        content.classList.toggle('collapsed');
                        if (toggleIcon) {
                            if (content.classList.contains('collapsed')) {
                                toggleIcon.classList.remove('fa-chevron-up');
                                toggleIcon.classList.add('fa-chevron-down');
                            } else {
                                toggleIcon.classList.remove('fa-chevron-down');
                                toggleIcon.classList.add('fa-chevron-up');
                            }
                        }
                    });
                }
                return;
            }
        });
        
        // Legacy function for backward compatibility
        function toggleScenario(header) {
            const body = header.nextElementSibling;
            requestAnimationFrame(() => {
                body.classList.toggle('expanded');
            });
        }

        // Toggle data table
        function toggleDataTable(header) {
            // header is now thead, find tbody and toggle it
            const tbody = header.parentElement.querySelector('tbody');
            if (tbody) {
                tbody.classList.toggle('collapsed');
                header.classList.toggle('collapsed');
            }
        }

        // Toggle doc string
        function toggleDocString(header) {
            const content = header.nextElementSibling;
            header.classList.toggle('collapsed');
            content.classList.toggle('collapsed');
        }

        // Toggle examples table
        function toggleExamplesTable(header) {
            // header is now thead, find tbody and toggle it
            const tbody = header.parentElement.querySelector('tbody');
            if (tbody) {
                tbody.classList.toggle('collapsed');
                header.classList.toggle('collapsed');
            }
        }

        // Toggle background section
        function toggleBackground(header) {
            const body = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            body.classList.toggle('expanded');
            
            // Update icon
            if (body.classList.contains('expanded')) {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            } else {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            }
        }

        // Toggle rule section
        function toggleRule(header) {
            const body = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            body.classList.toggle('expanded');
            
            // Update icon
            if (body.classList.contains('expanded')) {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            } else {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            }
        }

        // Toggle examples section
        function toggleExamples(header) {
            const content = header.nextElementSibling;
            const toggleIcon = header.querySelector('.toggle-icon');
            
            content.classList.toggle('collapsed');
            
            // Update icon
            if (content.classList.contains('collapsed')) {
                toggleIcon.classList.remove('fa-chevron-up');
                toggleIcon.classList.add('fa-chevron-down');
            } else {
                toggleIcon.classList.remove('fa-chevron-down');
                toggleIcon.classList.add('fa-chevron-up');
            }
        }

        // Debounced search functionality with highlighting and result count
        let searchTimeout;
        let searchResults = [];
        let currentSearchIndex = 0;
        const searchBox = document.getElementById('search-box');
        const searchResultCount = document.getElementById('search-result-count');
        const searchClearBtn = document.getElementById('search-clear-btn');
        const searchPrevBtn = document.getElementById('search-prev-btn');
        const searchNextBtn = document.getElementById('search-next-btn');
        
        function removeHighlights() {
            document.querySelectorAll('.search-highlight').forEach(highlight => {
                const parent = highlight.parentNode;
                parent.replaceChild(document.createTextNode(highlight.textContent), highlight);
                parent.normalize();
            });
        }
        
        function highlightText(element, searchTerm) {
            if (!searchTerm || element.children.length > 0) return;
            
            const text = element.textContent;
            const index = text.toLowerCase().indexOf(searchTerm);
            
            if (index >= 0) {
                const beforeMatch = text.substring(0, index);
                const match = text.substring(index, index + searchTerm.length);
                const afterMatch = text.substring(index + searchTerm.length);
                
                element.innerHTML = '';
                element.appendChild(document.createTextNode(beforeMatch));
                
                const mark = document.createElement('mark');
                mark.className = 'search-highlight';
                mark.textContent = match;
                element.appendChild(mark);
                
                element.appendChild(document.createTextNode(afterMatch));
            }
        }
        
        function performSearch() {
            const searchTerm = searchBox.value.toLowerCase().trim();
            
            // Show loader for search operation
            if (searchTerm && FEATURE_COUNT > 50) {
                showLoader('Searching features...', 50);
            }
            
            // Remove previous highlights
            removeHighlights();
            
            // Update global filter state
            activeFilters.searchTerm = searchTerm;
            
            // Render all lazy features if searching
            if (searchTerm && USE_LAZY_RENDERING) {
                document.querySelectorAll('.feature[data-lazy]').forEach(feature => {
                    renderFeatureContent(feature);
                });
            }
            
            // Defer heavy operations
            requestIdleCallback(() => {
                // Apply all filters (includes search)
                applyAllFilters();
                
                // Highlight search matches
                if (searchTerm) {
                    const features = document.querySelectorAll('.feature[data-feature-id]');
                    features.forEach(feature => {
                        if (feature.style.display !== 'none') {
                            // Highlight in feature title
                            const title = feature.querySelector('.feature-title h2');
                            if (title) highlightText(title, searchTerm);
                            
                            // Highlight in scenario names
                            feature.querySelectorAll('.scenario-title strong').forEach(el => {
                                highlightText(el, searchTerm);
                            });
                            
                            // SMART PROGRESSIVE DISCLOSURE: Auto-expand matched scenarios
                            const scenarios = feature.querySelectorAll('.scenario');
                            scenarios.forEach(scenario => {
                                if (scenario.style.display !== 'none') {
                                    const scenarioTitle = scenario.querySelector('.scenario-title strong');
                                    if (scenarioTitle && scenarioTitle.textContent.toLowerCase().includes(searchTerm)) {
                                        const scenarioHeader = scenario.querySelector('.scenario-header');
                                        if (scenarioHeader) {
                                            const scenarioBody = scenarioHeader.nextElementSibling;
                                            if (scenarioBody && scenarioBody.classList.contains('scenario-body')) {
                                                scenarioBody.classList.add('expanded');
                                            }
                                        }
                                    }
                                }
                            });
                        }
                    });
                }
                
                // Update search UI (counts, navigation buttons)
                updateSearchNavigationUI();
                
                // Hide loader
                hideLoader();
            }, { timeout: 200 });
        }
        
        function updateSearchNavigationUI() {
            const searchTerm = searchBox.value.trim();
            
            // Check if any filter is active
            const hasActiveFilter = activeFilters.status !== 'all' || 
                                   activeFilters.tags.length > 0 || 
                                   searchTerm;
            
            // Collect visible scenarios when any filter is active
            searchResults = [];
            if (hasActiveFilter) {
                // Get all visible scenarios (not hidden by filters)
                const allScenarios = document.querySelectorAll('.scenario');
                allScenarios.forEach(scenario => {
                    // Check if scenario is visible (not display: none)
                    const style = window.getComputedStyle(scenario);
                    if (style.display !== 'none') {
                        // Also check parent feature is visible
                        const parentFeature = scenario.closest('.feature[data-feature-id]');
                        if (parentFeature && !parentFeature.classList.contains('feature-hidden')) {
                            // If search term exists, also filter by text match
                            if (searchTerm) {
                                const scenarioText = scenario.textContent.toLowerCase();
                                if (scenarioText.includes(searchTerm.toLowerCase())) {
                                    searchResults.push(scenario);
                                }
                            } else {
                                // No search term - include all visible scenarios
                                searchResults.push(scenario);
                            }
                        }
                    }
                });
            }
            currentSearchIndex = 0;
            
            if (hasActiveFilter && searchResults.length > 0) {
                // Show count - different format for search vs filter-only
                if (searchTerm) {
                    searchResultCount.textContent = `${currentSearchIndex + 1} of ${searchResults.length}`;
                } else {
                    searchResultCount.textContent = `${searchResults.length} scenario${searchResults.length !== 1 ? 's' : ''}`;
                }
                searchResultCount.classList.add('visible');
                searchPrevBtn.classList.add('visible');
                searchNextBtn.classList.add('visible');
                
                // Only show clear button if there's search text
                if (searchTerm) {
                    searchClearBtn.classList.add('visible');
                } else {
                    searchClearBtn.classList.remove('visible');
                }
                
                // Update button states
                searchPrevBtn.disabled = currentSearchIndex === 0;
                searchNextBtn.disabled = currentSearchIndex === searchResults.length - 1;
                
                // Scroll to first result
                const mainContent = document.getElementById('main-content');
                if (mainContent) {
                    mainContent.scrollTop = 0;
                }
            } else {
                searchResultCount.classList.remove('visible');
                searchClearBtn.classList.remove('visible');
                searchPrevBtn.classList.remove('visible');
                searchNextBtn.classList.remove('visible');
            }
        }
        
        function updateSearchUI() {
            const searchTerm = searchBox.value.trim();
            
            // Check if any filter is active
            const hasActiveFilter = activeFilters.status !== 'all' || 
                                   activeFilters.tags.length > 0 || 
                                   searchTerm;
            
            if (hasActiveFilter && searchResults.length > 0) {
                // Show count - different format for search vs filter-only
                if (searchTerm) {
                    searchResultCount.textContent = `${currentSearchIndex + 1} of ${searchResults.length}`;
                } else {
                    searchResultCount.textContent = `${currentSearchIndex + 1} of ${searchResults.length}`;
                }
                searchResultCount.classList.add('visible');
                searchPrevBtn.classList.add('visible');
                searchNextBtn.classList.add('visible');
                
                // Only show clear button if there's search text
                if (searchTerm) {
                    searchClearBtn.classList.add('visible');
                } else {
                    searchClearBtn.classList.remove('visible');
                }
                
                // Update button states
                searchPrevBtn.disabled = currentSearchIndex === 0;
                searchNextBtn.disabled = currentSearchIndex === searchResults.length - 1;
                
                // NOTE: Do NOT scroll here - let navigateSearchResults handle scrolling
            } else {
                searchResultCount.classList.remove('visible');
                searchClearBtn.classList.remove('visible');
                searchPrevBtn.classList.remove('visible');
                searchNextBtn.classList.remove('visible');
            }
        }
        
        function navigateSearchResults(direction) {
            if (searchResults.length === 0) return;
            
            // Update index
            if (direction === 'next') {
                currentSearchIndex = Math.min(currentSearchIndex + 1, searchResults.length - 1);
            } else {
                currentSearchIndex = Math.max(currentSearchIndex - 1, 0);
            }
            
            // Get the current scenario from searchResults
            const currentScenario = searchResults[currentSearchIndex];
            
            // Find the parent feature and ensure it's visible
            const currentFeature = currentScenario.closest('.feature[data-feature-id]');
            if (currentFeature) {
                currentFeature.classList.remove('feature-hidden');
            }
            
            // Update UI
            updateSearchUI();
            
            // Scroll to the scenario in main content
            requestAnimationFrame(() => {
                const mainContent = document.getElementById('main-content');
                if (mainContent && currentScenario) {
                    // Get the scenario header for scrolling (more visible target)
                    const scenarioHeader = currentScenario.querySelector('.scenario-header');
                    const scrollTarget = scenarioHeader || currentScenario;
                    
                    // Calculate position relative to main content container
                    const targetRect = scrollTarget.getBoundingClientRect();
                    const mainContentRect = mainContent.getBoundingClientRect();
                    const relativeTop = targetRect.top - mainContentRect.top + mainContent.scrollTop;
                    
                    // Scroll to position the scenario near the top with some padding
                    mainContent.scrollTo({
                        top: Math.max(0, relativeTop - 80),
                        behavior: 'smooth'
                    });
                    
                    // Also update sidebar selection
                    if (currentFeature) {
                        const featureId = currentFeature.getAttribute('data-feature-id');
                        if (featureId) {
                            selectFeature(featureId);
                        }
                    }
                }
            });
        }
        
        searchBox.addEventListener('input', function(e) {
            clearTimeout(searchTimeout);
            // Performance: Increased debounce for large reports
            const debounceDelay = PERF_LARGE_REPORT ? 400 : 300;
            searchTimeout = setTimeout(() => {
                // Performance: Use requestAnimationFrame for smooth UI updates
                requestAnimationFrame(() => {
                    performSearch();
                });
            }, debounceDelay);
        });
        
        // Clear search functionality
        searchClearBtn.addEventListener('click', function() {
            searchBox.value = '';
            performSearch();
            searchBox.focus();
        });
        
        // Navigation buttons
        searchPrevBtn.addEventListener('click', function() {
            navigateSearchResults('prev');
        });
        
        searchNextBtn.addEventListener('click', function() {
            navigateSearchResults('next');
        });
        
        // Keyboard shortcuts for search
        searchBox.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && searchBox.value) {
                e.preventDefault();
                searchBox.value = '';
                performSearch();
            } else if (e.key === 'Enter' && searchBox.value) {
                e.preventDefault();
                if (e.shiftKey) {
                    navigateSearchResults('prev');
                } else {
                    navigateSearchResults('next');
                }
            }
        });

        // ============================================
        // GLOBAL FILTER STATE
        // ============================================
        let activeFilters = {
            status: 'all',      // 'all', 'passed', 'failed', 'skipped', 'untested'
            tags: [],           // Array of tag strings
            searchTerm: ''      // Current search text
        };

        // ============================================
        // HELPER FUNCTIONS FOR FILTERING
        // ============================================
        
        function updateSidebarForFilter(visibleFeatureIds, preserveFolderState = false) {
            const sidebarItems = document.querySelectorAll('.feature-item');
            let matchCount = 0;
            
            sidebarItems.forEach(item => {
                const featureId = item.getAttribute('data-feature-id');
                if (visibleFeatureIds.has(featureId)) {
                    item.style.display = 'flex';
                    item.style.opacity = '1';
                    matchCount++;
                } else {
                    // Hide non-matching features completely
                    item.style.display = 'none';
                    item.style.opacity = '1';
                }
            });
            
            // Update folder visibility and expansion
            document.querySelectorAll('.folder').forEach(folder => {
                const visibleItems = Array.from(folder.querySelectorAll('.feature-item'))
                    .filter(item => item.style.display !== 'none');
                
                if (visibleItems.length > 0) {
                    folder.style.display = 'block';
                    folder.style.opacity = '1';
                    // Only auto-expand folders when actively filtering, not when clearing filters
                    if (!preserveFolderState) {
                        folder.classList.remove('collapsed');
                    }
                } else {
                    // Hide empty folders completely
                    folder.style.display = 'none';
                    folder.style.opacity = '1';
                }
            });
        }
        
        function showEmptyStateIfNeeded(visibleCount, filter) {
    const mainContent = document.getElementById('main-content');
    let emptyState = document.getElementById('empty-state-message');

    if (visibleCount === 0) {
        if (!emptyState) {
            emptyState = document.createElement('div');
            emptyState.id = 'empty-state-message';
            emptyState.className = 'empty-state';
            emptyState.style.cssText = `
                text-align: center;
                padding: 60px 20px;
                color: var(--text-secondary);
            `;
            mainContent.insertBefore(emptyState, mainContent.firstChild);
        }

        const filterText = filter === 'all' ? 'scenarios' :
                           filter === 'passed' ? 'passed scenarios' :
                           filter === 'failed' ? 'failed scenarios' :
                           filter === 'skipped' ? 'skipped scenarios' :
                           filter === 'untested' ? 'untested scenarios' : 'scenarios';

        emptyState.innerHTML =
            '<div style=""font-size: 48px; margin-bottom: 16px;"">&#128269;</div>' +
            '<h2 style=""margin: 0 0 8px 0;"">No ' + filterText + ' found</h2>' +
            '<p style=""margin: 0 0 24px 0;"">Try clearing all filters to view all scenarios.</p>' +
            '<button onclick=""clearAllFilters();"" style=""' +
                'padding: 10px 24px;' +
                'background: var(--primary-color);' +
                'color: white;' +
                'border: none;' +
                'border-radius: 6px;' +
                'cursor: pointer;' +
                'font-size: 14px;' +
            '"">Clear All Filters</button>';

        emptyState.style.display = 'block';
    } else if (emptyState) {
        emptyState.style.display = 'none';
    }
}

        
        function announceToScreenReader(message) {
            let announcer = document.getElementById('screen-reader-announcer');
            
            if (!announcer) {
                announcer = document.createElement('div');
                announcer.id = 'screen-reader-announcer';
                announcer.setAttribute('role', 'status');
                announcer.setAttribute('aria-live', 'polite');
                announcer.setAttribute('aria-atomic', 'true');
                announcer.style.cssText = `
                    position: absolute;
                    left: -10000px;
                    width: 1px;
                    height: 1px;
                    overflow: hidden;
                `;
                document.body.appendChild(announcer);
            }
            
            announcer.textContent = message;
            
            setTimeout(() => {
                announcer.textContent = '';
            }, 100);
        }

        // ============================================
        // FILTER BY STATUS (INTEGRATED WITH ALL FILTERS)
        // ============================================
        function filterByStatus(filter) {
            // Update global filter state
            activeFilters.status = filter;
            
            // Update active state on filter buttons
            document.querySelectorAll('.filter-btn[data-filter]').forEach(b => {
                if (b.dataset.filter === filter) {
                    b.classList.add('active');
                    b.setAttribute('aria-pressed', 'true');
                } else {
                    b.classList.remove('active');
                    b.setAttribute('aria-pressed', 'false');
                }
            });

            // Render all lazy features if filtering by specific status
            // This ensures scenarios are available for filtering
            if (filter !== 'all' && USE_LAZY_RENDERING) {
                const lazyFeatures = document.querySelectorAll('.feature[data-lazy]');
                lazyFeatures.forEach(feature => {
                    renderFeatureContent(feature);
                });
                // Use double requestAnimationFrame to ensure DOM is fully updated
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        applyAllFilters();
                        // Auto-select first visible feature if any
                        selectFirstVisibleFeature();
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    });
                });
                return;
            }

            // Apply all filters (this respects search, tags, and status together)
            applyAllFilters();
            
            // Auto-select first visible feature if any
            selectFirstVisibleFeature();
            
            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        // ============================================
        // CLEAR ALL FILTERS
        // ============================================
        function clearAllFilters() {
            // Reset filter state
            activeFilters.status = 'all';
            activeFilters.tags = [];
            activeFilters.searchTerm = '';
            
            // Clear search box
            if (searchBox) searchBox.value = '';
            
            // Reset filter buttons
            document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
                if (btn.dataset.filter === 'all') {
                    btn.classList.add('active');
                    btn.setAttribute('aria-pressed', 'true');
                } else {
                    btn.classList.remove('active');
                    btn.setAttribute('aria-pressed', 'false');
                }
            });
            
            // Reset tag dropdown
            const tagFilter = document.getElementById('tag-filter');
            if (tagFilter) tagFilter.value = 'all';
            
            // Remove highlights
            removeHighlights();
            
            // Apply filters (will show all)
            applyAllFilters();
            
            // Update search UI
            updateSearchNavigationUI();
            
            // Announce to screen reader
            announceToScreenReader('All filters cleared. Showing all scenarios.');
        }
        
        // ============================================
        // MASTER FILTER FUNCTION (COMBINED FILTERS)
        // ============================================
        function applyAllFilters() {
            const features = document.querySelectorAll('.feature[data-feature-id]');
            let totalVisibleScenarios = 0;
            const visibleFeatureIds = new Set();
            
            // Check if no filters are active (show all)
            const noFiltersActive = activeFilters.status === 'all' && 
                                    activeFilters.tags.length === 0 && 
                                    !activeFilters.searchTerm;
            
            features.forEach(feature => {
                const scenarios = feature.querySelectorAll('.scenario');
                const isLazyFeature = feature.hasAttribute('data-lazy');
                let visibleInFeature = 0;
                
                // For lazy-loaded features with no filters active, show them without checking scenarios
                if (isLazyFeature && noFiltersActive) {
                    feature.style.display = 'block';
                    const featureId = feature.getAttribute('data-feature-id');
                    if (featureId) visibleFeatureIds.add(featureId);
                    return; // Skip scenario iteration for lazy features when showing all
                }
                
                scenarios.forEach(scenario => {
                    // Check status filter
                    const status = scenario.dataset.status;
                    const matchesStatus = activeFilters.status === 'all' || status === activeFilters.status;
                    
                    // Check tag filters (AND logic: scenario must have ALL selected tags)
                    // Get scenario-level tags (from scenario body's tags div)
                    const scenarioBody = scenario.querySelector('.scenario-body');
                    const scenarioTagsDiv = scenarioBody ? scenarioBody.querySelector(':scope > .tags') : null;
                    const scenarioTags = scenarioTagsDiv ? Array.from(scenarioTagsDiv.querySelectorAll('.tag'))
                        .map(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            return clone.textContent.trim();
                        }) : [];
                    
                    // Get rule-level tags if scenario is inside a rule
                    const parentRule = scenario.closest('.rule');
                    const ruleBody = parentRule ? parentRule.querySelector('.rule-body') : null;
                    const ruleTagsDiv = ruleBody ? ruleBody.querySelector(':scope > .tags') : null;
                    const ruleTags = ruleTagsDiv ? Array.from(ruleTagsDiv.querySelectorAll('.tag'))
                        .map(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            return clone.textContent.trim();
                        }) : [];
                    
                    // Get feature-level tags (from feature body's tags div, not from scenarios)
                    const featureBody = feature.querySelector('.feature-body');
                    const featureTagsDiv = featureBody ? featureBody.querySelector(':scope > .tags') : null;
                    const featureHeaderTags = featureTagsDiv ? Array.from(featureTagsDiv.querySelectorAll('.tag'))
                        .map(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            return clone.textContent.trim();
                        }) : [];
                    
                    // Get Example-level tags (tags on Examples tables within Scenario Outlines)
                    const examplesTags = [];
                    const examplesTagsDivs = scenario.querySelectorAll('.examples-tags');
                    examplesTagsDivs.forEach(examplesTagsDiv => {
                        Array.from(examplesTagsDiv.querySelectorAll('.tag')).forEach(t => {
                            const clone = t.cloneNode(true);
                            const icon = clone.querySelector('i');
                            if (icon) icon.remove();
                            examplesTags.push(clone.textContent.trim());
                        });
                    });
                    
                    // Combine all tags: scenario + rule + feature + examples level
                    const allTags = [...new Set([...scenarioTags, ...ruleTags, ...featureHeaderTags, ...examplesTags])];
                    
                    const matchesTags = activeFilters.tags.length === 0 || 
                        activeFilters.tags.every(filterTag => 
                            allTags.some(scenarioTag => 
                                scenarioTag.toLowerCase().includes(filterTag.toLowerCase())
                            )
                        );
                    
                    // Check search term (search in feature title and scenario names only)
                    let matchesSearch = !activeFilters.searchTerm;
                    if (activeFilters.searchTerm && !matchesSearch) {
                        const searchLower = activeFilters.searchTerm.toLowerCase();
                        // Check feature title
                        const featureTitle = feature.querySelector('.feature-title h2');
                        if (featureTitle && featureTitle.textContent.toLowerCase().includes(searchLower)) {
                            matchesSearch = true;
                        }
                        // Check scenario name
                        if (!matchesSearch) {
                            const scenarioTitle = scenario.querySelector('.scenario-title strong');
                            if (scenarioTitle && scenarioTitle.textContent.toLowerCase().includes(searchLower)) {
                                matchesSearch = true;
                            }
                        }
                    }
                    
                    // Apply AND logic: scenario must match ALL active filters
                    const matchesAllFilters = matchesStatus && matchesTags && matchesSearch;
                    
                    if (matchesAllFilters) {
                        scenario.style.display = 'block';
                        visibleInFeature++;
                        totalVisibleScenarios++;
                    } else {
                        scenario.style.display = 'none';
                    }
                });
                
                // Handle feature visibility
                // When no filters are active, show all features regardless of scenario count
                if (noFiltersActive) {
                    feature.style.display = 'block';
                    const featureId = feature.getAttribute('data-feature-id');
                    if (featureId) visibleFeatureIds.add(featureId);
                    // Show all scenarios when no filters active
                    scenarios.forEach(scenario => {
                        scenario.style.display = 'block';
                    });
                } else if (visibleInFeature > 0) {
                    feature.style.display = 'block';
                    // Remove feature-hidden class so it's visible in content area
                    feature.classList.remove('feature-hidden');
                    const featureId = feature.getAttribute('data-feature-id');
                    if (featureId) visibleFeatureIds.add(featureId);
                } else {
                    feature.style.display = 'none';
                    // Add feature-hidden class for consistency
                    feature.classList.add('feature-hidden');
                }
            });
            
            // Update UI elements
            // Preserve folder collapse state when clearing all filters (no active filters)
            updateSidebarForFilter(visibleFeatureIds, noFiltersActive);
            showEmptyStateIfNeeded(totalVisibleScenarios, activeFilters.status);
            
            // Update navigation UI for filtered results
            updateSearchNavigationUI();
            
            // Announce results
            const announcement = `Filter applied. Showing ${totalVisibleScenarios} scenario${totalVisibleScenarios !== 1 ? 's' : ''} in ${visibleFeatureIds.size} feature${visibleFeatureIds.size !== 1 ? 's' : ''}`;
            announceToScreenReader(announcement);
        }

        // Filter by tag
        function filterByTag(tag) {
            // Update global filter state
            if (tag === 'all') {
                activeFilters.tags = [];
            } else {
                activeFilters.tags = [tag];
            }
            
            // Render all lazy features if filtering by tag
            if (tag !== 'all' && USE_LAZY_RENDERING) {
                // Get all lazy features and render them
                const lazyFeatures = document.querySelectorAll('.feature[data-lazy]');
                lazyFeatures.forEach(feature => {
                    renderFeatureContent(feature);
                });
                
                // Use double requestAnimationFrame to ensure DOM is fully updated
                // First rAF waits for current frame, second ensures all DOM updates are processed
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        applyAllFilters();
                        // Auto-select first visible feature if any
                        selectFirstVisibleFeature();
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    });
                });
                return;
            }
            
            // Apply all filters (this respects search, status, and tags together)
            applyAllFilters();
            
            // Auto-select first visible feature if any
            selectFirstVisibleFeature();
            
            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        // Helper function to select the first visible feature after filtering
        function selectFirstVisibleFeature() {
            // Find the first visible feature in the content area
            const visibleFeature = document.querySelector('.feature[data-feature-id]:not(.feature-hidden)');
            if (visibleFeature) {
                const featureId = visibleFeature.getAttribute('data-feature-id');
                if (featureId) {
                    // Update sidebar active state
                    document.querySelectorAll('.feature-item').forEach(item => {
                        item.classList.remove('active');
                    });
                    const sidebarItem = document.querySelector('.feature-item[data-feature-id=""' + featureId + '""]');
                    if (sidebarItem) {
                        sidebarItem.classList.add('active');
                        // Ensure the sidebar item is visible (expand parent folders)
                        let parent = sidebarItem.parentElement;
                        while (parent) {
                            if (parent.classList.contains('folder-content')) {
                                const folder = parent.closest('.folder');
                                if (folder && folder.classList.contains('collapsed')) {
                                    folder.classList.remove('collapsed');
                                    folder.setAttribute('aria-expanded', 'true');
                                }
                            }
                            parent = parent.parentElement;
                        }
                        // Scroll sidebar item into view
                        sidebarItem.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                    currentFeatureId = featureId;
                }
            }
        }

        // Screen reader announcement helper
        function announceToScreenReader(message) {
            const announcement = document.createElement('div');
            announcement.setAttribute('role', 'status');
            announcement.setAttribute('aria-live', 'polite');
            announcement.className = 'sr-only';
            announcement.textContent = message;
            document.body.appendChild(announcement);
            setTimeout(() => announcement.remove(), 1000);
        }
        
        // Keyboard navigation for feature items
        function handleFeatureKeydown(event, featureId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                selectFeature(featureId);
            }
        }
        
        // Keyboard navigation for folder headers
        function handleFolderKeydown(event, folderId) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                toggleFolder(folderId);
            }
        }
        
        // Keyboard shortcuts
        document.addEventListener('keydown', function(e) {
            // Ctrl/Cmd + K for search focus
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                document.getElementById('search-box').focus();
            }
            // Ctrl/Cmd + E for toggle expand/collapse all
            if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
                e.preventDefault();
                toggleExpandAll();
            }
            // Ctrl/Cmd + B for toggle sidebar
            if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                toggleSidebar();
            }
        });

        // Theme Configuration - Generated from ThemeConfig.cs
        const themes = " + GenerateJavaScriptThemes() + @";

        // Change theme function
        function changeTheme(themeName) {
            const theme = themes[themeName];
            if (!theme) return;

            const root = document.documentElement;
            root.style.setProperty('--primary-color', theme.primaryColor);
            root.style.setProperty('--primary-gradient', theme.primaryGradient);
            root.style.setProperty('--success-color', theme.successColor);
            root.style.setProperty('--danger-color', theme.dangerColor);
            root.style.setProperty('--warning-color', theme.warningColor);
            root.style.setProperty('--info-color', theme.infoColor);
            root.style.setProperty('--bg-color', theme.bgColor);
            root.style.setProperty('--card-bg', theme.cardBg);
            root.style.setProperty('--text-color', theme.textColor);
            root.style.setProperty('--text-secondary', theme.textSecondary);
            root.style.setProperty('--border-color', theme.borderColor);
            root.style.setProperty('--hover-bg', theme.hoverBg);
            root.style.setProperty('--accent-color', theme.accentColor);
            root.style.setProperty('--focus-ring', theme.focusRing);
            root.style.setProperty('--shadow-color', theme.shadowColor);
            root.style.setProperty('--code-bg', theme.codeBg);

            // Save theme preference
            localStorage.setItem('bdd-theme', themeName);
        }

        // ============================================
        // LAZY RENDERING SYSTEM
        // ============================================
        let featureDataCache = null;
        let renderedFeatures = new Set();
        
        function loadFeatureData() {
            if (!USE_LAZY_RENDERING) return null;
            if (featureDataCache) return featureDataCache;
            
            const dataElement = document.getElementById('feature-data');
            if (dataElement) {
                try {
                    featureDataCache = JSON.parse(dataElement.textContent);
                    console.log('✓ Loaded data for ' + featureDataCache.features.length + ' features');
                } catch (e) {
                    console.error('Failed to parse feature data:', e);
                    featureDataCache = { features: [] };
                }
            }
            return featureDataCache;
        }
        
        function renderFeatureContent(featureElement) {
            const featureIndex = parseInt(featureElement.getAttribute('data-feature-index'));
            if (renderedFeatures.has(featureIndex)) return;
            
            const data = loadFeatureData();
            if (!data || !data.features[featureIndex]) {
                console.error('Failed to load feature data for index:', featureIndex);
                return;
            }
            
            const featureHtml = data.features[featureIndex].html;
            
            // Create a temporary container to parse the HTML
            const temp = document.createElement('div');
            temp.innerHTML = featureHtml;
            const newFeatureElement = temp.firstElementChild;
            
            // Replace the placeholder with the actual feature content
            if (newFeatureElement && featureElement.parentNode) {
                // Preserve original hidden state if any
                const wasHidden = featureElement.classList.contains('feature-hidden');
                if (wasHidden) {
                    newFeatureElement.classList.add('feature-hidden');
                } else {
                    newFeatureElement.classList.remove('feature-hidden');
                }
                
                // Ensure the new element doesn't have lazy-loading classes/attributes
                newFeatureElement.classList.remove('lazy-feature');
                newFeatureElement.removeAttribute('data-lazy');
                
                featureElement.parentNode.replaceChild(newFeatureElement, featureElement);
                console.log('✓ Rendered feature:', featureIndex);
            }
            
            renderedFeatures.add(featureIndex);
        }
        
        function initLazyRendering() {
            if (!USE_LAZY_RENDERING) return;
            
            const lazyFeatures = document.querySelectorAll('.lazy-feature[data-lazy]');
            if (lazyFeatures.length === 0) return;
            
            console.log('⚡ Initializing lazy rendering for ' + lazyFeatures.length + ' features');
            
            // Render first 10 features immediately for instant visibility
            const initialRenderCount = Math.min(10, lazyFeatures.length);
            for (let i = 0; i < initialRenderCount; i++) {
                renderFeatureContent(lazyFeatures[i]);
            }
            
            // Use IntersectionObserver for remaining features
            const observerOptions = {
                root: null,
                rootMargin: '500px 0px', // Load 500px before entering viewport
                threshold: 0
            };
            
            const lazyObserver = new IntersectionObserver(function(entries) {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const feature = entry.target;
                        if (feature.hasAttribute('data-lazy')) {
                            renderFeatureContent(feature);
                            lazyObserver.unobserve(feature); // Stop observing after rendering
                        }
                    }
                });
            }, observerOptions);
            
            // Observe features that weren't initially rendered
            for (let i = initialRenderCount; i < lazyFeatures.length; i++) {
                lazyObserver.observe(lazyFeatures[i]);
            }
        }
        
        // Load saved theme on page load
        document.addEventListener('DOMContentLoaded', function() {
            // Initialize lazy rendering first (if enabled)
            initLazyRendering();
            
            // Attach filter event listeners after lazy rendering is initialized
            document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
                btn.addEventListener('click', function() {
                    const filter = this.dataset.filter;
                    // Use clearAllFilters for 'all' button, filterByStatus for specific filters
                    if (filter === 'all') {
                        clearAllFilters();
                    } else {
                        filterByStatus(filter);
                    }
                });
            });
            
            // Clear All button
            const clearAllBtn = document.getElementById('clear-all-filters-btn');
            if (clearAllBtn) {
                clearAllBtn.addEventListener('click', function() {
                    clearAllFilters();
                });
            }
            
            const tagFilterDropdown = document.getElementById('tag-filter');
            if (tagFilterDropdown) {
                tagFilterDropdown.addEventListener('change', function() {
                    const selectedTag = this.value;
                    filterByTag(selectedTag);
                });
            }
            
            const savedTheme = localStorage.getItem('bdd-theme') || 'purple';
            const themeSelector = document.getElementById('theme-selector');
            themeSelector.value = savedTheme;
            changeTheme(savedTheme);
            
            // Populate tag filter dropdown
            const tagFilter = document.getElementById('tag-filter');
            const allTags = new Set();
            
            // First try to get tags from JSON data (for lazy-loaded reports)
            // This ensures tags are available even before features are rendered
            const featureDataScript = document.getElementById('feature-data');
            if (featureDataScript && USE_LAZY_RENDERING) {
                try {
                    const featureData = JSON.parse(featureDataScript.textContent);
                    if (featureData.allTags && Array.isArray(featureData.allTags)) {
                        featureData.allTags.forEach(tag => allTags.add(tag));
                    }
                } catch (e) {
                    console.warn('Could not parse feature data for tags:', e);
                }
            }
            
            // Also collect tags from already-rendered DOM elements (for non-lazy or as fallback)
            document.querySelectorAll('.tag').forEach(tag => {
                // Extract tag text, excluding icon by cloning and removing it
                const clone = tag.cloneNode(true);
                const icon = clone.querySelector('i');
                if (icon) {
                    icon.remove();
                }
                const tagText = clone.textContent.trim();
                if (tagText) allTags.add(tagText);
            });
            
            const sortedTags = Array.from(allTags).sort((a, b) => a.localeCompare(b, undefined, {sensitivity: 'base'}));
            sortedTags.forEach(tag => {
                const option = document.createElement('option');
                option.value = tag;
                option.textContent = tag;
                tagFilter.appendChild(option);
            });
            
            // Scroll to top button
            // OPTIMIZED: Use passive listener for better scroll performance
            const scrollToTopBtn = document.getElementById('scroll-to-top');
            
            window.addEventListener('scroll', function() {
                if (window.pageYOffset > 300) {
                    scrollToTopBtn.classList.add('visible');
                } else {
                    scrollToTopBtn.classList.remove('visible');
                }
            }, { passive: true });
            
            scrollToTopBtn.addEventListener('click', function() {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            });
            
            // Setup IntersectionObserver to update sidebar active state (optimized)
            if (!USE_LAZY_RENDERING) {
                // Only use scenario observer for non-lazy reports
                setupScenarioObserver();
            } else {
                // For lazy reports, use simpler feature-level tracking
                setupFeatureLevelObserver();
            }
        });

        // ============================================
        // SIDEBAR NAVIGATION & MASTER-DETAIL
        // ============================================
        
        // OPTIMIZED: Throttled observer for lazy-rendered reports (feature-level only)
        // FIX: Prevents flickering by throttling updates and batching DOM operations
        function setupFeatureLevelObserver() {
            let updateScheduled = false;
            let lastFeatureId = null;
            
            const options = {
                root: null,
                rootMargin: '-20% 0px -60% 0px',
                threshold: [0, 0.25, 0.5]  // Multiple thresholds for better accuracy
            };
            
            const observer = new IntersectionObserver(function(entries) {
                // Find the most visible feature (highest intersection ratio)
                let mostVisibleFeature = null;
                let maxRatio = 0;
                
                entries.forEach(entry => {
                    if (entry.isIntersecting && entry.intersectionRatio > maxRatio) {
                        maxRatio = entry.intersectionRatio;
                        mostVisibleFeature = entry.target;
                    }
                });
                
                if (!mostVisibleFeature) return;
                
                const featureId = mostVisibleFeature.getAttribute('data-feature-id');
                
                // Skip if same feature or update already scheduled (throttling)
                if (featureId === lastFeatureId || updateScheduled) return;
                
                lastFeatureId = featureId;
                updateScheduled = true;
                
                // Throttle: Maximum once per animation frame
                requestAnimationFrame(() => {
                    updateSidebarActive(featureId);
                    // Allow next update after short delay (debounce effect)
                    setTimeout(() => {
                        updateScheduled = false;
                    }, 100);
                });
            }, options);
            
            // Only observe feature containers (much lighter than all scenarios)
            document.querySelectorAll('.feature[data-feature-id]').forEach(feature => {
                observer.observe(feature);
            });
        }
        
        // OPTIMIZED: Throttled scenario-level observer for smaller reports
        function setupScenarioObserver() {
            let updateScheduled = false;
            let lastFeatureId = null;
            
            const options = {
                root: null,
                rootMargin: '-20% 0px -60% 0px',
                threshold: [0, 0.25, 0.5]
            };
            
            const observer = new IntersectionObserver(function(entries) {
                let mostVisibleScenario = null;
                let maxRatio = 0;
                
                entries.forEach(entry => {
                    if (entry.isIntersecting && entry.intersectionRatio > maxRatio) {
                        maxRatio = entry.intersectionRatio;
                        mostVisibleScenario = entry.target;
                    }
                });
                
                if (!mostVisibleScenario) return;
                
                const featureId = mostVisibleScenario.getAttribute('data-feature-id');
                
                // Skip if same feature or update already scheduled
                if (featureId === lastFeatureId || updateScheduled) return;
                
                lastFeatureId = featureId;
                updateScheduled = true;
                
                requestAnimationFrame(() => {
                    updateSidebarActive(featureId);
                    setTimeout(() => {
                        updateScheduled = false;
                    }, 100);
                });
            }, options);
            
            // Observe all scenarios
            document.querySelectorAll('.scenario[data-feature-id]').forEach(scenario => {
                observer.observe(scenario);
            });
        }
        
        // OPTIMIZED: Batched DOM operations to prevent layout thrashing
        // FIX: This is the main fix for flickering - separates DOM reads and writes
        function updateSidebarActive(featureId) {
            // PHASE 1: Batch all DOM reads (no forced layouts)
            const itemsToUpdate = [];
            const allItems = document.querySelectorAll('.feature-item');
            
            allItems.forEach(item => {
                const itemFeatureId = item.getAttribute('data-feature-id');
                const shouldBeActive = itemFeatureId === featureId;
                const isCurrentlyActive = item.classList.contains('active');
                
                // Only track items that need changes
                if (shouldBeActive !== isCurrentlyActive) {
                    itemsToUpdate.push({ item, shouldBeActive });
                }
            });
            
            // Early exit if no changes needed (common case during scroll)
            if (itemsToUpdate.length === 0) return;
            
            // PHASE 2: Batch all DOM writes in requestAnimationFrame
            requestAnimationFrame(() => {
                // Update all classes in one batch
                itemsToUpdate.forEach(({ item, shouldBeActive }) => {
                    if (shouldBeActive) {
                        item.classList.add('active');
                    } else {
                        item.classList.remove('active');
                    }
                });
                
                // Handle sidebar scrolling separately
                const activeItem = itemsToUpdate.find(x => x.shouldBeActive)?.item;
                if (activeItem) {
                    // Expand parent folders
                    expandParentFolders(activeItem);
                    
                    // Defer scrolling to next frame to avoid layout in this frame
                    requestAnimationFrame(() => {
                        scrollSidebarToItem(activeItem);
                    });
                }
            });
        }
        
        // HELPER: Separate function for sidebar scrolling (prevents layout thrashing)
        function scrollSidebarToItem(activeItem) {
            const sidebar = document.getElementById('sidebar');
            const sidebarNav = sidebar?.querySelector('nav');
            if (!sidebarNav || !activeItem) return;
            
            // Batch all reads first
            const itemTop = activeItem.offsetTop;
            const itemBottom = itemTop + activeItem.offsetHeight;
            const sidebarTop = sidebarNav.scrollTop;
            const sidebarBottom = sidebarTop + sidebarNav.clientHeight;
            
            // Single write operation
            if (itemTop < sidebarTop || itemBottom > sidebarBottom) {
                activeItem.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
        
        // Note: selectFeature and toggleFolder are defined at the top of the script
        // to ensure they're available for inline onclick handlers
        
        // Expand all parent folders to make an element visible
        function expandParentFolders(element) {
            let parent = element.parentElement;
            while (parent) {
                if (parent.classList.contains('folder-content')) {
                    const folder = parent.closest('.folder');
                    if (folder && folder.classList.contains('collapsed')) {
                        folder.classList.remove('collapsed');
                        folder.setAttribute('aria-expanded', 'true');
                    }
                }
                parent = parent.parentElement;
            }
        }
        
        // Toggle all folders with dynamic icon and tooltip
        function toggleAllFolders() {
            const toggleBtn = document.getElementById('toggle-folders-btn');
            const currentState = toggleBtn.getAttribute('data-state');
            const icon = toggleBtn.querySelector('i');
            
            if (currentState === 'expanded') {
                // Collapse all folders
                document.querySelectorAll('.folder:not(.collapsed)').forEach(folder => {
                    folder.classList.add('collapsed');
                    folder.setAttribute('aria-expanded', 'false');
                });
                
                // Update button state
                toggleBtn.setAttribute('data-state', 'collapsed');
                toggleBtn.setAttribute('title', 'Expand All Folders');
                icon.className = 'fas fa-folder-closed';
            } else {
                // Expand all folders
                document.querySelectorAll('.folder.collapsed').forEach(folder => {
                    folder.classList.remove('collapsed');
                    folder.setAttribute('aria-expanded', 'true');
                });
                
                // Update button state
                toggleBtn.setAttribute('data-state', 'expanded');
                toggleBtn.setAttribute('title', 'Collapse All Folders');
                icon.className = 'fas fa-folder-open';
            }
        }
        
        // Legacy functions kept for backward compatibility (can be called from other places)
        function expandAllFolders() {
            document.querySelectorAll('.folder.collapsed').forEach(folder => {
                folder.classList.remove('collapsed');
                folder.setAttribute('aria-expanded', 'true');
            });
            
            // Update toggle button if exists
            const toggleBtn = document.getElementById('toggle-folders-btn');
            if (toggleBtn) {
                toggleBtn.setAttribute('data-state', 'expanded');
                toggleBtn.setAttribute('title', 'Collapse All Folders');
                toggleBtn.querySelector('i').className = 'fas fa-folder-open';
            }
        }
        
        function collapseAllFolders() {
            document.querySelectorAll('.folder:not(.collapsed)').forEach(folder => {
                folder.classList.add('collapsed');
                folder.setAttribute('aria-expanded', 'false');
            });
            
            // Update toggle button if exists
            const toggleBtn = document.getElementById('toggle-folders-btn');
            if (toggleBtn) {
                toggleBtn.setAttribute('data-state', 'collapsed');
                toggleBtn.setAttribute('title', 'Expand All Folders');
                toggleBtn.querySelector('i').className = 'fas fa-folder-closed';
            }
        }
        
        // Collapse folders at a specific depth level (for performance)
        function collapseFoldersAtDepth(minDepth) {
            document.querySelectorAll('.folder').forEach(folder => {
                const depth = parseInt(folder.getAttribute('data-depth') || '0');
                if (depth >= minDepth) {
                    folder.classList.add('collapsed');
                    folder.setAttribute('aria-expanded', 'false');
                }
            });
        }
        
        // Sidebar Toggle Function
        function toggleSidebar() {
            const sidebar = document.getElementById('sidebar');
            const floatingToggle = document.getElementById('floating-sidebar-toggle');
            const sidebarToggle = document.getElementById('sidebar-toggle');
            const isCollapsed = sidebar.classList.toggle('collapsed');
            
            // Update ARIA attributes
            sidebar.setAttribute('aria-hidden', isCollapsed);
            if (sidebarToggle) {
                sidebarToggle.setAttribute('aria-expanded', !isCollapsed);
            }
            
            // Update icon in sidebar toggle button
            const icon = document.querySelector('#sidebar-toggle i');
            if (icon) {
                if (isCollapsed) {
                    icon.className = 'fas fa-angles-right';
                } else {
                    icon.className = 'fas fa-angles-left';
                }
            }
            
            // Toggle floating button visibility
            if (floatingToggle) {
                if (isCollapsed) {
                    floatingToggle.classList.add('visible');
                    floatingToggle.setAttribute('aria-hidden', 'false');
                } else {
                    floatingToggle.classList.remove('visible');
                    floatingToggle.setAttribute('aria-hidden', 'true');
                }
            }
            
            // Save state
            localStorage.setItem('bdd-sidebar-collapsed', isCollapsed);
        }
        
        // Sidebar Toggle (inside sidebar)
        document.getElementById('sidebar-toggle')?.addEventListener('click', toggleSidebar);
        
        // Floating Toggle Button (visible when collapsed)
        document.getElementById('floating-sidebar-toggle')?.addEventListener('click', toggleSidebar);
        
        // Sidebar Resize
        const resizer = document.querySelector('.resizer');
        const sidebarElement = document.getElementById('sidebar');
        let isResizing = false;
        
        if (resizer && sidebarElement) {
            resizer.addEventListener('mousedown', function(e) {
                isResizing = true;
                resizer.classList.add('resizing');
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';
                e.preventDefault();
            });
            
            document.addEventListener('mousemove', function(e) {
                if (!isResizing) return;
                
                const layoutContainer = document.querySelector('.layout-container');
                if (!layoutContainer) return;
                
                const containerRect = layoutContainer.getBoundingClientRect();
                const newWidth = e.clientX - containerRect.left;
                
                // Dynamic max width based on container size (max 40% of container)
                const minWidth = 200;
                const maxWidth = Math.min(500, containerRect.width * 0.4);
                
                if (newWidth >= minWidth && newWidth <= maxWidth) {
                    sidebarElement.style.width = newWidth + 'px';
                }
            });
            
            document.addEventListener('mouseup', function() {
                if (isResizing) {
                    isResizing = false;
                    resizer.classList.remove('resizing');
                    document.body.style.cursor = '';
                    document.body.style.userSelect = '';
                    
                    // Save width
                    localStorage.setItem('bdd-sidebar-width', sidebarElement.style.width);
                }
            });
        }
        
        // Enhanced Keyboard Shortcuts
        document.addEventListener('keydown', function(e) {
            // Cmd/Ctrl + B: Toggle sidebar
            if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
                e.preventDefault();
                toggleSidebar();
            }
        });
        
        // Load Saved State on Page Load
        document.addEventListener('DOMContentLoaded', function() {
            // Restore sidebar width (only on desktop)
            if (window.innerWidth > 768) {
                const savedWidth = localStorage.getItem('bdd-sidebar-width');
                if (savedWidth && sidebar) {
                    const width = parseInt(savedWidth);
                    const layoutContainer = document.querySelector('.layout-container');
                    if (layoutContainer) {
                        const maxWidth = Math.min(500, layoutContainer.offsetWidth * 0.4);
                        // Ensure saved width is within valid range
                        if (width >= 200 && width <= maxWidth) {
                            sidebar.style.width = savedWidth;
                        }
                    }
                }
            }
            
            // Restore sidebar collapsed state
            const isCollapsed = localStorage.getItem('bdd-sidebar-collapsed') === 'true';
            const floatingToggle = document.getElementById('floating-sidebar-toggle');
            
            if (isCollapsed && sidebar) {
                sidebar.classList.add('collapsed');
                const icon = document.querySelector('#sidebar-toggle i');
                if (icon) {
                    icon.className = 'fas fa-angles-right';
                }
                // Show floating toggle button when sidebar is collapsed
                if (floatingToggle) {
                    floatingToggle.classList.add('visible');
                }
            }
            
            // Restore last viewed feature
            const lastFeature = localStorage.getItem('bdd-last-feature');
            if (lastFeature) {
                selectFeature(lastFeature);
            }
        });
        
        // Handle window resize
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(function() {
                if (sidebar && window.innerWidth > 768) {
                    const layoutContainer = document.querySelector('.layout-container');
                    if (layoutContainer) {
                        const currentWidth = parseInt(sidebar.style.width || '280');
                        const maxWidth = Math.min(500, layoutContainer.offsetWidth * 0.4);
                        
                        // Adjust sidebar width if it exceeds new maximum
                        if (currentWidth > maxWidth) {
                            sidebar.style.width = maxWidth + 'px';
                            localStorage.setItem('bdd-sidebar-width', sidebar.style.width);
                        }
                    }
                } else if (window.innerWidth <= 768 && sidebar) {
                    // Reset inline width on mobile
                    sidebar.style.width = '';
                }
            }, 250);
        });
    </script>";
    }

    /// <summary>
    /// Generates JavaScript object literal from ThemeConfig.cs themes
    /// </summary>
    private static string GenerateJavaScriptThemes()
    {
        var js = new System.Text.StringBuilder();
        js.AppendLine("{");
        
        var themes = ThemeConfig.Themes;
        var themeNames = themes.Keys.ToList();
        
        for (int i = 0; i < themeNames.Count; i++)
        {
            var themeName = themeNames[i];
            var theme = themes[themeName];
            
            js.AppendLine($"            {themeName}: {{");
            js.AppendLine($"                primaryColor: '{theme.PrimaryColor}',");
            js.AppendLine($"                primaryGradient: '{theme.PrimaryGradient}',");
            js.AppendLine($"                successColor: '{theme.SuccessColor}',");
            js.AppendLine($"                dangerColor: '{theme.DangerColor}',");
            js.AppendLine($"                warningColor: '{theme.WarningColor}',");
            js.AppendLine($"                infoColor: '{theme.InfoColor}',");
            js.AppendLine($"                bgColor: '{theme.BgColor}',");
            js.AppendLine($"                cardBg: '{theme.CardBg}',");
            js.AppendLine($"                textColor: '{theme.TextColor}',");
            js.AppendLine($"                textSecondary: '{theme.TextSecondary}',");
            js.AppendLine($"                borderColor: '{theme.BorderColor}',");
            js.AppendLine($"                hoverBg: '{theme.HoverBg}',");
            js.AppendLine($"                accentColor: '{theme.AccentColor}',");
            js.AppendLine($"                focusRing: '{theme.FocusRing}',");
            js.AppendLine($"                shadowColor: '{theme.ShadowColor}',");
            js.AppendLine($"                codeBg: '{theme.CodeBg}'");
            js.Append($"            }}");
            
            if (i < themeNames.Count - 1)
                js.AppendLine(",");
            else
                js.AppendLine();
        }
        
        js.Append("        }");
        return js.ToString();
    }
}
