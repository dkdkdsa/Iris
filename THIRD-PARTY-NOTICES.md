# Third-Party Notices

Iris is licensed under the MIT License (see [LICENSE](LICENSE)). It depends on the
third-party components listed below. Each remains under its own license, and those
licenses are reproduced in full in this file.

Some components ship native binaries (`SDL2.dll`, `cimgui.dll`, `stbi.dll` and their
Linux/macOS equivalents) that are copied next to a built game. Redistributing a game
built with Iris therefore redistributes those binaries, and this notice file should be
distributed with it.

## Components

| Component | Version | License | Copyright |
| --- | --- | --- | --- |
| [Box2D.NET](https://github.com/ikpil/Box2D.NET) | 3.1.654 | MIT | Erin Catto; Choi Ikpil |
| [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) | 2.2.9 | MIT | Juna Meinhold |
| [HexaGen.Runtime](https://github.com/JunaMeinhold/HexaGen) | 1.1.21 | MIT | Juna Meinhold |
| [Dear ImGui](https://github.com/ocornut/imgui) (native `cimgui`, shipped by Hexa.NET.ImGui) | — | MIT | Omar Cornut |
| [NAudio](https://github.com/naudio/NAudio) (`NAudio`, `.Core`, `.Asio`, `.Midi`, `.Wasapi`, `.WinMM`) | 2.3.0 | MIT | Mark Heath & contributors |
| [Silk.NET](https://github.com/dotnet/Silk.NET) (`Silk.NET.SDL`, `.Core`, `.Maths`) | 2.23.0 | MIT | .NET Foundation and Contributors |
| [SDL2](https://www.libsdl.org/) (native, shipped by `Ultz.Native.SDL`) | 2.32.10 | zlib | Sam Lantinga |
| [StbiSharp](https://www.nuget.org/packages/StbiSharp) | 1.2.1 | BSD 3-Clause | Thomas Müller |
| [StbTrueTypeSharp](https://github.com/rds1983/StbSharp) | 1.26.12 | Public Domain | StbSharp contributors |
| [stb](https://github.com/nothings/stb) (`stb_image`, `stb_truetype`) | — | MIT / Public Domain | Sean Barrett |
| System.IO.Hashing | 10.0.10 | MIT | .NET Foundation and Contributors |
| Microsoft.Extensions.DependencyModel | 9.0.9 | MIT | .NET Foundation and Contributors |
| Microsoft.DotNet.PlatformAbstractions | 3.1.6 | MIT | .NET Foundation and Contributors |

## MIT License

Applies to Box2D.NET, Hexa.NET.ImGui, HexaGen.Runtime, Dear ImGui, NAudio, Silk.NET,
System.IO.Hashing, Microsoft.Extensions.DependencyModel,
Microsoft.DotNet.PlatformAbstractions, and to the MIT option of the stb libraries.

Copyright holders, as applicable:

```
Copyright (c) 2022 Erin Catto
Copyright (c) 2025 Choi Ikpil
Copyright (c) 2023 Juna Meinhold
Copyright (c) 2014-2025 Omar Cornut
Copyright (c) 2020 Mark Heath
Copyright (c) .NET Foundation and Contributors
Copyright (c) 2017 Sean Barrett
```

```
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

## zlib License

Applies to SDL2 (native binaries shipped by `Ultz.Native.SDL`).

```
Simple DirectMedia Layer
Copyright (C) 1997-2025 Sam Lantinga <slouken@libsdl.org>

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
```

## BSD 3-Clause License

Applies to StbiSharp.

```
BSD 3-Clause License

Copyright (c) 2019, Thomas Müller <thomas@tom94.net>
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

## Public Domain

StbTrueTypeSharp is released into the public domain by its authors. The upstream
`stb_truetype.h` and `stb_image.h` by Sean Barrett are dual-licensed as MIT (reproduced
above) or public domain (Unlicense), at the user's option.

```
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute this
software, either in source code form or as a compiled binary, for any purpose,
commercial or non-commercial, and by any means.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```
