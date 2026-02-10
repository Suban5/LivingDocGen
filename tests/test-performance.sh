#!/bin/bash
# Performance Testing Script for BDD Living Documentation
# Usage: ./test-performance.sh [feature-count]

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Get script directory for absolute paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

FEATURE_COUNT=${1:-100}
TEST_RESULTS_DIR="$SCRIPT_DIR/performance-test-results"
REPORT_OUTPUT="$TEST_RESULTS_DIR/perf-test-report.html"

echo "======================================"
echo "BDD Living Doc Performance Test"
echo "======================================"
echo ""
echo "Testing with $FEATURE_COUNT features..."
echo ""

# Step 1: Create test directory
mkdir -p "$TEST_RESULTS_DIR"

# Step 2: Generate test features if needed
echo "📝 Generating test features..."
if [ ! -d "$TEST_RESULTS_DIR/features" ]; then
    mkdir -p "$TEST_RESULTS_DIR/features"
    
    # Generate test feature files
    for i in $(seq 1 $FEATURE_COUNT); do
        cat > "$TEST_RESULTS_DIR/features/test_feature_$i.feature" <<EOF
Feature: Test Feature $i
  As a tester
  I want to test performance
  So that I can validate scroll smoothness

  @smoke @performance
  Scenario: Test Scenario 1 for Feature $i
    Given I have opened the application
    When I perform action $i
    Then I should see result $i

  @regression
  Scenario: Test Scenario 2 for Feature $i
    Given the system is ready
    When I execute test $i
    Then the system responds correctly

  Scenario Outline: Test Scenario Outline for Feature $i
    Given I have <item>
    When I use <action>
    Then I get <result>

    Examples:
      | item    | action   | result  |
      | A       | process  | success |
      | B       | validate | pass    |
      | C       | execute  | done    |
EOF
    done
    echo "✅ Generated $FEATURE_COUNT test features"
fi

# Step 3: Generate mock test results
echo "📊 Generating mock test results..."
if [ ! -f "$TEST_RESULTS_DIR/test-results.trx" ]; then
    # Create a simple TRX file with some results
    cat > "$TEST_RESULTS_DIR/test-results.trx" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Times creation="2026-02-10T10:00:00.000Z" />
  <TestSettings name="default" id="00000000-0000-0000-0000-000000000000">
    <Deployment runDeploymentRoot="TestResults" />
  </TestSettings>
  <Results>
  </Results>
  <TestDefinitions>
  </TestDefinitions>
  <TestEntries>
  </TestEntries>
  <ResultSummary outcome="Completed">
    <Counters total="0" executed="0" passed="0" failed="0" />
  </ResultSummary>
</TestRun>
EOF
    echo "✅ Generated mock test results"
fi

# Step 4: Build the project
echo "🔨 Building LivingDocGen..."
dotnet build "$PROJECT_ROOT/LivingDocGen.sln" --configuration Release > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# Step 5: Generate the report
echo "📄 Generating HTML report..."
dotnet run --project "$PROJECT_ROOT/src/LivingDocGen.CLI/LivingDocGen.CLI.csproj" -- \
    generate "$TEST_RESULTS_DIR/features" "$TEST_RESULTS_DIR" \
    --output "$REPORT_OUTPUT" \
    --title "Performance Test Report ($FEATURE_COUNT features)" \
    --theme blue \
    --verbose

if [ $? -eq 0 ]; then
    echo "✅ Report generated: $REPORT_OUTPUT"
else
    echo "❌ Report generation failed"
    exit 1
fi

# Step 6: Get report size
REPORT_SIZE=$(du -h "$REPORT_OUTPUT" | cut -f1)
echo ""
echo "📏 Report size: $REPORT_SIZE"

# Step 7: Create performance testing HTML wrapper
echo "🧪 Creating performance test wrapper..."
cat > "$TEST_RESULTS_DIR/performance-tester.html" <<'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>Performance Tester</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            margin-top: 0;
        }
        .metrics {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }
        .metric-card {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #6366f1;
        }
        .metric-label {
            font-size: 12px;
            text-transform: uppercase;
            color: #666;
            margin-bottom: 8px;
        }
        .metric-value {
            font-size: 32px;
            font-weight: bold;
            color: #333;
        }
        .metric-unit {
            font-size: 16px;
            color: #666;
        }
        .status-good {
            border-left-color: #10b981;
        }
        .status-warning {
            border-left-color: #f59e0b;
        }
        .status-bad {
            border-left-color: #ef4444;
        }
        button {
            background: #6366f1;
            color: white;
            border: none;
            padding: 12px 24px;
            border-radius: 6px;
            font-size: 16px;
            cursor: pointer;
            margin-right: 10px;
        }
        button:hover {
            background: #4f46e5;
        }
        button:disabled {
            background: #9ca3af;
            cursor: not-allowed;
        }
        #results {
            margin-top: 30px;
        }
        .info {
            background: #eff6ff;
            border-left: 4px solid #3b82f6;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }
        iframe {
            width: 100%;
            height: 600px;
            border: 1px solid #ddd;
            border-radius: 8px;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>⚡ BDD Living Doc Performance Tester</h1>
        
        <div class="info">
            <strong>Instructions:</strong>
            <ol>
                <li>Click "Start Test" below</li>
                <li>Wait 5 seconds for initialization</li>
                <li>The system will automatically scroll through the report</li>
                <li>Performance metrics will be collected and displayed</li>
                <li>Review results to validate no flickering occurs</li>
            </ol>
        </div>

        <div>
            <button id="startBtn" onclick="startTest()">Start Performance Test</button>
            <button id="stopBtn" onclick="stopTest()" disabled>Stop Test</button>
            <button onclick="resetTest()">Reset</button>
        </div>

        <div id="results" style="display: none;">
            <h2>Real-time Performance Metrics</h2>
            <div class="metrics">
                <div class="metric-card" id="fps-card">
                    <div class="metric-label">Frame Rate</div>
                    <div class="metric-value"><span id="fps">--</span> <span class="metric-unit">fps</span></div>
                </div>
                <div class="metric-card" id="cls-card">
                    <div class="metric-label">Layout Shifts (CLS)</div>
                    <div class="metric-value" id="cls">--</div>
                </div>
                <div class="metric-card" id="tasks-card">
                    <div class="metric-label">Long Tasks</div>
                    <div class="metric-value"><span id="longTasks">--</span> <span class="metric-unit">tasks</span></div>
                </div>
                <div class="metric-card">
                    <div class="metric-label">Test Duration</div>
                    <div class="metric-value"><span id="duration">--</span> <span class="metric-unit">s</span></div>
                </div>
            </div>
            
            <div id="verdict" style="padding: 20px; margin-top: 20px; border-radius: 8px; font-size: 18px; font-weight: bold;">
            </div>
        </div>

        <iframe id="reportFrame" src="perf-test-report.html" style="display: none;"></iframe>
    </div>

    <script>
        let testRunning = false;
        let startTime = 0;
        let frameCount = 0;
        let fps = 0;
        let lastFrameTime = performance.now();
        let fpsReadings = [];
        let cumulativeLayoutShift = 0;
        let longTaskCount = 0;
        let animationId = null;
        let durationInterval = null;

        function updateFPS() {
            if (!testRunning) return;
            
            frameCount++;
            const now = performance.now();
            const delta = now - lastFrameTime;
            
            if (delta >= 1000) {
                fps = Math.round((frameCount * 1000) / delta);
                fpsReadings.push(fps);
                
                document.getElementById('fps').textContent = fps;
                
                // Update card color
                const fpsCard = document.getElementById('fps-card');
                fpsCard.className = 'metric-card ' + 
                    (fps >= 55 ? 'status-good' : fps >= 45 ? 'status-warning' : 'status-bad');
                
                frameCount = 0;
                lastFrameTime = now;
            }
            
            animationId = requestAnimationFrame(updateFPS);
        }

        function startTest() {
            testRunning = true;
            startTime = Date.now();
            frameCount = 0;
            fpsReadings = [];
            cumulativeLayoutShift = 0;
            longTaskCount = 0;
            lastFrameTime = performance.now();
            
            document.getElementById('startBtn').disabled = true;
            document.getElementById('stopBtn').disabled = false;
            document.getElementById('results').style.display = 'block';
            document.getElementById('reportFrame').style.display = 'block';
            document.getElementById('verdict').style.display = 'none';
            
            // Monitor CLS
            const clsObserver = new PerformanceObserver((list) => {
                for (const entry of list.getEntries()) {
                    if (!entry.hadRecentInput) {
                        cumulativeLayoutShift += entry.value;
                        document.getElementById('cls').textContent = cumulativeLayoutShift.toFixed(4);
                        
                        const clsCard = document.getElementById('cls-card');
                        clsCard.className = 'metric-card ' +
                            (cumulativeLayoutShift < 0.1 ? 'status-good' : 
                             cumulativeLayoutShift < 0.25 ? 'status-warning' : 'status-bad');
                    }
                }
            });
            
            if (PerformanceObserver.supportedEntryTypes.includes('layout-shift')) {
                clsObserver.observe({ entryTypes: ['layout-shift'] });
            }
            
            // Monitor long tasks
            const taskObserver = new PerformanceObserver((list) => {
                longTaskCount += list.getEntries().length;
                document.getElementById('longTasks').textContent = longTaskCount;
                
                const tasksCard = document.getElementById('tasks-card');
                tasksCard.className = 'metric-card ' +
                    (longTaskCount < 5 ? 'status-good' : 
                     longTaskCount < 15 ? 'status-warning' : 'status-bad');
            });
            
            if (PerformanceObserver.supportedEntryTypes.includes('longtask')) {
                taskObserver.observe({ entryTypes: ['longtask'] });
            }
            
            // Start FPS monitoring
            updateFPS();
            
            // Update duration
            durationInterval = setInterval(() => {
                const duration = ((Date.now() - startTime) / 1000).toFixed(1);
                document.getElementById('duration').textContent = duration;
            }, 100);
            
            // Auto-scroll the iframe
            setTimeout(() => {
                autoScrollReport();
            }, 2000);
        }

        function autoScrollReport() {
            const iframe = document.getElementById('reportFrame');
            const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
            const mainContent = iframeDoc.getElementById('main-content');
            
            if (!mainContent || !testRunning) return;
            
            let scrollPos = 0;
            const maxScroll = mainContent.scrollHeight - mainContent.clientHeight;
            const scrollStep = 2; // pixels per frame
            
            function scrollFrame() {
                if (!testRunning || scrollPos >= maxScroll) {
                    if (scrollPos >= maxScroll) {
                        // Scroll back to top and repeat
                        scrollPos = 0;
                        mainContent.scrollTop = 0;
                        setTimeout(scrollFrame, 1000);
                    }
                    return;
                }
                
                scrollPos += scrollStep;
                mainContent.scrollTop = scrollPos;
                requestAnimationFrame(scrollFrame);
            }
            
            scrollFrame();
        }

        function stopTest() {
            testRunning = false;
            document.getElementById('startBtn').disabled = false;
            document.getElementById('stopBtn').disabled = true;
            
            if (animationId) {
                cancelAnimationFrame(animationId);
            }
            
            if (durationInterval) {
                clearInterval(durationInterval);
            }
            
            showVerdict();
        }

        function showVerdict() {
            const avgFps = fpsReadings.reduce((a, b) => a + b, 0) / fpsReadings.length;
            const minFps = Math.min(...fpsReadings);
            
            const verdict = document.getElementById('verdict');
            verdict.style.display = 'block';
            
            let passed = true;
            let message = '<h3>Test Results:</h3><ul style="text-align: left;">';
            
            // FPS check
            if (avgFps >= 55) {
                message += '<li>✅ Average FPS: ' + avgFps.toFixed(1) + ' (Target: 55+)</li>';
            } else {
                message += '<li>❌ Average FPS: ' + avgFps.toFixed(1) + ' (Target: 55+)</li>';
                passed = false;
            }
            
            if (minFps >= 45) {
                message += '<li>✅ Minimum FPS: ' + minFps + ' (Target: 45+)</li>';
            } else {
                message += '<li>❌ Minimum FPS: ' + minFps + ' (Target: 45+)</li>';
                passed = false;
            }
            
            // CLS check
            if (cumulativeLayoutShift < 0.1) {
                message += '<li>✅ CLS: ' + cumulativeLayoutShift.toFixed(4) + ' (Target: <0.1)</li>';
            } else if (cumulativeLayoutShift < 0.25) {
                message += '<li>⚠️ CLS: ' + cumulativeLayoutShift.toFixed(4) + ' (Target: <0.1, Acceptable: <0.25)</li>';
            } else {
                message += '<li>❌ CLS: ' + cumulativeLayoutShift.toFixed(4) + ' (Target: <0.1)</li>';
                passed = false;
            }
            
            // Long tasks check
            if (longTaskCount < 5) {
                message += '<li>✅ Long Tasks: ' + longTaskCount + ' (Target: <5)</li>';
            } else if (longTaskCount < 15) {
                message += '<li>⚠️ Long Tasks: ' + longTaskCount + ' (Target: <5, Acceptable: <15)</li>';
            } else {
                message += '<li>❌ Long Tasks: ' + longTaskCount + ' (Target: <5)</li>';
            }
            
            message += '</ul>';
            
            if (passed) {
                message += '<div style="margin-top: 15px; color: #10b981;">🎉 ALL TESTS PASSED - No flickering detected!</div>';
                verdict.style.background = '#d1fae5';
                verdict.style.border = '2px solid #10b981';
            } else {
                message += '<div style="margin-top: 15px; color: #ef4444;">⚠️ PERFORMANCE ISSUES DETECTED</div>';
                verdict.style.background = '#fee2e2';
                verdict.style.border = '2px solid #ef4444';
            }
            
            verdict.innerHTML = message;
        }

        function resetTest() {
            location.reload();
        }
    </script>
</body>
</html>
EOF

echo "✅ Performance tester created"
echo ""
echo "======================================"
echo "✨ Test Setup Complete!"
echo "======================================"
echo ""
echo "📂 Test files location: $TEST_RESULTS_DIR"
echo "📄 Generated report: $REPORT_OUTPUT"
echo "🧪 Performance tester: $TEST_RESULTS_DIR/performance-tester.html"
echo ""
echo "Next steps:"
echo "1. Open $TEST_RESULTS_DIR/performance-tester.html in your browser"
echo "2. Click 'Start Performance Test'"
echo "3. Review the metrics after test completes"
echo "4. Check for:"
echo "   - FPS > 55 (average)"
echo "   - CLS < 0.1"
echo "   - No visible flickering"
echo ""
echo -e "${GREEN}Target Metrics:${NC}"
echo "  • Average FPS: ≥ 55 fps"
echo "  • Minimum FPS: ≥ 45 fps"
echo "  • CLS Score: < 0.1"
echo "  • Long Tasks: < 5"
echo ""

# Open in browser (macOS only)
if [[ "$OSTYPE" == "darwin"* ]]; then
    echo "🌐 Opening performance tester in browser..."
    open "$TEST_RESULTS_DIR/performance-tester.html"
else
    echo "Please open the following file in your browser manually:"
    echo "  $TEST_RESULTS_DIR/performance-tester.html"
fi

echo ""
echo -e "${YELLOW}Note:${NC} Run this script with different feature counts to test scalability:"
echo "  ./test-performance.sh 50   # Small report"
echo "  ./test-performance.sh 100  # Medium report"
echo "  ./test-performance.sh 200  # Large report"
echo ""
