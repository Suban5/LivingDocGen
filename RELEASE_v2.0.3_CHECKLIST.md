# Release v2.0.3 Checklist

**Release Date:** January 22, 2026  
**Packages:** LivingDocGen.Tool (CLI) & LivingDocGen.Reqnroll.Integration

---

## ✅ Files Updated

### Version Numbers
- [x] `/src/LivingDocGen.CLI/LivingDocGen.CLI.csproj` - Version updated to 2.0.3
- [x] `/src/LivingDocGen.Reqnroll.Integration/LivingDocGen.Reqnroll.Integration.csproj` - Version updated to 2.0.3

### CHANGELOGs
- [x] `/src/LivingDocGen.CLI/CHANGELOG.md` - Unreleased moved to [2.0.3] section
- [x] `/src/LivingDocGen.Reqnroll.Integration/CHANGELOG.md` - Unreleased moved to [2.0.3] section

### README Files
- [x] `/src/LivingDocGen.CLI/README.md` - Added "What's New in v2.0.3" section
- [x] `/src/LivingDocGen.Reqnroll.Integration/README.md` - Added "What's New in v2.0.3" section

---

## 📋 Pre-Release Steps

### 1. Build & Test
```bash
# Build the solution
cd /Users/subandhyako/MasterThesis/LivingDocGen
dotnet build --configuration Release

# Run tests
dotnet test

# Test CLI tool locally
dotnet pack src/LivingDocGen.CLI/LivingDocGen.CLI.csproj -c Release
dotnet tool install --global --add-source ./src/LivingDocGen.CLI/bin/Release LivingDocGen.Tool

# Test Reqnroll integration
dotnet pack src/LivingDocGen.Reqnroll.Integration/LivingDocGen.Reqnroll.Integration.csproj -c Release
```

### 2. Verify Package Metadata
Check that .csproj files have correct metadata:
- [x] Version: 2.0.3
- [x] Authors: Suban Dhyako
- [x] Description: Accurate and up-to-date
- [x] PackageTags: Relevant keywords
- [x] PackageProjectUrl: https://github.com/suban5/LivingDocGen
- [x] License: MIT
- [x] README.md included in package

### 3. Generate Release Notes

Copy from CHANGELOGs for GitHub release:

**LivingDocGen.Tool v2.0.3**
```markdown
## Phase 2 Performance Optimizations

### Changed
- Lazy rendering: Reports with 50+ features load progressively on scroll
- Initial page load 87% faster for 200-feature reports (12s → 1.5s)
- Time to interactive 86% faster (18s → 2.5s)
- Smooth 60fps scrolling and instant toggle response (<16ms)
- Memory usage reduced by 66% (350MB → 120MB)
- Unified event delegation for all toggle operations
- Optimized IntersectionObserver reduces overhead by 80-90%

### What This Means
- ✅ Handles 200+ feature files with 500+ scenarios smoothly
- ✅ No page freezing or unresponsive toggles
- ✅ Automatic activation (no configuration needed)
- ✅ Backward compatible (small reports unchanged)
```

**LivingDocGen.Reqnroll.Integration v2.0.3**
```markdown
## Phase 2 Performance Optimizations

### Changed
- Lazy rendering: Reports with 50+ features load progressively on scroll
- Handles 200+ feature files with 500+ scenarios smoothly
- Initial page load 87% faster (12s → 1.5s)
- Time to interactive 86% faster (18s → 2.5s)
- Memory efficient: 66% reduction in browser memory usage (350MB → 120MB)
- Smooth 60fps scrolling with instant toggle response
- Automatic activation for reports with 50+ features

### What This Means
- ✅ Large test suites generate fast, responsive reports
- ✅ No configuration required - optimizations activate automatically
- ✅ Perfect for CI/CD pipelines with comprehensive test coverage
```

---

## 🚀 Release Process

### 1. Commit Changes
```bash
git add .
git commit -F COMMIT_MESSAGE.txt
```

### 2. Create Git Tag
```bash
git tag -a v2.0.3 -m "Release v2.0.3 - Phase 2 Performance Optimizations"
git push origin main
git push origin v2.0.3
```

### 3. Build Release Packages
```bash
# Clean previous builds
dotnet clean -c Release

# Build and pack CLI
dotnet pack src/LivingDocGen.CLI/LivingDocGen.CLI.csproj \
  -c Release \
  -o ./nupkg \
  /p:Version=2.0.3

# Build and pack Reqnroll Integration
dotnet pack src/LivingDocGen.Reqnroll.Integration/LivingDocGen.Reqnroll.Integration.csproj \
  -c Release \
  -o ./nupkg \
  /p:Version=2.0.3
```

### 4. Publish to NuGet
```bash
# Set your NuGet API key (one-time setup)
# Get key from: https://www.nuget.org/account/apikeys
export NUGET_API_KEY="your-api-key-here"

# Publish CLI tool
dotnet nuget push ./nupkg/LivingDocGen.Tool.2.0.3.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json

# Publish Reqnroll Integration
dotnet nuget push ./nupkg/LivingDocGen.Reqnroll.Integration.2.0.3.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### 5. Create GitHub Release
1. Go to: https://github.com/suban5/LivingDocGen/releases/new
2. Tag: `v2.0.3`
3. Title: `v2.0.3 - Phase 2 Performance Optimizations`
4. Description: Use release notes from step 3 above
5. Attach .nupkg files (optional)
6. Publish release

---

## 📢 Post-Release

### 1. Verify NuGet Packages
- [ ] Check https://www.nuget.org/packages/LivingDocGen.Tool/
- [ ] Check https://www.nuget.org/packages/LivingDocGen.Reqnroll.Integration/
- [ ] Verify version 2.0.3 is listed
- [ ] Check that README displays correctly

### 2. Test Installation
```bash
# Uninstall old version
dotnet tool uninstall -g LivingDocGen.Tool

# Install new version
dotnet tool install -g LivingDocGen.Tool --version 2.0.3

# Verify version
LivingDocGen --version
```

### 3. Update Documentation (if needed)
- [ ] Update main README.md if installation instructions changed
- [ ] Update docs/FAQ.md if new common questions
- [ ] Update examples if output format changed

### 4. Announce Release (Optional)
- [ ] Blog post
- [ ] Twitter/LinkedIn
- [ ] Reddit (r/dotnet, r/csharp)
- [ ] Dev.to article

---

## 🐛 Rollback Plan (If Issues Found)

### If Critical Bug Found:
```bash
# Unlist broken version on NuGet (don't delete)
# This hides it from search but existing users can still download

# Option 1: Fix and release v2.0.4 quickly
# Option 2: Revert to v2.0.2 in documentation

# Users can downgrade with:
dotnet tool update -g LivingDocGen.Tool --version 2.0.2
```

---

## 📊 Release Metrics to Track

After 1 week, check:
- [ ] NuGet download count for v2.0.3
- [ ] GitHub issues related to performance
- [ ] User feedback on large reports (200+ features)
- [ ] Any regression reports

---

## ✅ Release Completed

- [ ] All packages published to NuGet
- [ ] GitHub release created with notes
- [ ] Git tags pushed
- [ ] Documentation updated
- [ ] Installation tested
- [ ] Announcement made (optional)

**Release Manager:** _______________  
**Date Completed:** _______________

---

## 📝 Notes

### Key Improvements in v2.0.3
- Solves the page freezing issue with 200+ feature files
- Scenario toggles now work instantly (was completely broken)
- Memory efficient (66% reduction)
- Automatic - no user configuration required

### Breaking Changes
- None - fully backward compatible

### Known Limitations
- Search and filtering work on rendered content only (progressive)
- For 1000+ features, consider Phase 3 optimizations (future)

### Dependencies
- All internal dependencies bundled (CLI and Reqnroll packages are self-contained)
- No external dependency updates needed
