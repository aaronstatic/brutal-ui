# Third-Party Notices

brutal-ui contains code that comes from the projects in this file. Each project keeps its own license.
The `LICENSE` file at the repository root applies to all other code of brutal-ui.

---

## Yoga

The flexbox layout engine of `BrutalUi.Core` is a structural port of the Yoga layout algorithm to C#.
The fixture tests of the engine are ports of the Yoga generated tests.
No Yoga file, package, or binary is part of brutal-ui.

- **Repository:** https://github.com/facebook/yoga
- **Tag:** `v3.2.1`
- **Commit:** `042f5013152eb81c1552dec945b88f7b95ca350f`
- **License:** MIT
- **Copyright:** The source files of Yoga at this tag carry "Copyright (c) Meta Platforms, Inc. and affiliates."
  The `LICENSE` file at this tag carries "Copyright (c) Facebook, Inc. and its affiliates." The full text of that file follows the list of derived files.

### Derived files

Each file in this list starts with a header that names Yoga, the tag, and the Yoga source files.

- `src/BrutalUi.Core/Layout/LayoutMath.cs`
- `src/BrutalUi.Core/Layout/LayoutEngine.cs`
- `src/BrutalUi.Core/Layout/LayoutEngine.Lines.cs`
- `src/BrutalUi.Core/Layout/LayoutEngine.Absolute.cs`
- `src/BrutalUi.Core/Layout/LayoutCache.cs`
- `tests/BrutalUi.Core.Tests/Layout/FlexDirectionFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MarginFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/PaddingFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/BorderFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/FlexFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MinMaxDimensionFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/JustifyContentFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/DimensionFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/AlignItemsFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/AlignSelfFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/AbsolutePositionFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/DisplayFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/RelativePositionFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/PercentageFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/AutoFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/FlexWrapFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/AlignContentFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MeasureFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/SizeOverflowFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/RoundingFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MeasureCacheFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MeasureModeFixtureTests.cs`
- `tests/BrutalUi.Core.Tests/Layout/MeasureStub.cs`

### The Yoga LICENSE file at tag v3.2.1

```text
MIT License

Copyright (c) Facebook, Inc. and its affiliates.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
