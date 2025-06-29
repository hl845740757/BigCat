#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// csharp代码生成器
///
/// 1.这是默认的生成器，用户有特殊需求时，可参考该生成器代码。
/// 2.用户如果需要使用不可变集合，请将不可变集合注册到<see cref="DSRepository"/>，默认为Commons库中的不可变集合。
/// 3.默认情况下List使用<code>CollectionUtil.SequenceEqual</code>方法，而Set和字典使用<code>CollectionUtil.DataEquals</code>方法。
/// 4.如果默认的逻辑不满足要求，可将类型标注为<code>partial</code>，自行扩展equals和hashcode。
/// 5.如果反复增删set和字典的数据，hashcode无法保证一致 —— 只增不减的情况下才能保证hashcode和equals一致。
/// </summary>
public class CodeGenerator
{
    private static readonly AttributeSpec processorInfo = ToolUtil.NewProcessorInfoAnnotation(typeof(CodeGenerator));

    private readonly DSRepository _repository;
    private readonly CodeGeneratorCfg _cfg;
    private readonly LinkedHashSet<string> _fileNames;
    private readonly CodeGeneratorHelper _helper;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">脚本仓库</param>
    /// <param name="cfg">生成器配置</param>
    /// <param name="fileNames">需要生成代码的文件</param>
    public CodeGenerator(DSRepository repository, CodeGeneratorCfg cfg, ICollection<string> fileNames) {
        _repository = repository;
        _cfg = cfg;
        _helper = new CodeGeneratorHelper(repository, cfg, processorInfo);
        _fileNames = new LinkedHashSet<string>(fileNames);
    }

    public void Execute() {
        if (!Directory.Exists(_cfg.outPath)) {
            Directory.CreateDirectory(_cfg.outPath);
        }
        foreach (string fileName in _fileNames) {
            string fileSimpleName = fileName;
            if (fileSimpleName.EndsWith(".ds")) {
                fileSimpleName = fileSimpleName.Substring(0, fileSimpleName.Length - 3);
            }
            DSFile? dsFile = _repository.GetFile(fileSimpleName);
            if (dsFile == null) {
                throw new InvalidOperationException("ds file not found: " + fileSimpleName);
            }
            string? csharpNamespace = dsFile.GetOption(DSKeywords.CSHARP_NAMESPACE);
            if (string.IsNullOrEmpty(csharpNamespace)) {
                throw new InvalidOperationException("csharpNamespace is absent" + dsFile.FileName);
            }
            if (_cfg.combine) {
                NamespaceSpec.Builder nsBuilder = NamespaceSpec.NewBuilder(csharpNamespace);
                foreach (var element in dsFile.EnclosedElements) {
                    if (!element.Kind.IsNamedType()) continue; // inst
                    DSNamedType namedType = (DSNamedType)element;
                    try {
                        TypeSpec.Builder typeBuilder = _helper.Generate(namedType);
                        nsBuilder.AddSpec(typeBuilder.Build());
                    }
                    catch (Exception ex) {
                        throw new Exception($"file: {fileName}, type: {namedType.FullName}", ex);
                    }
                }
                ToolUtil.WriteToFile(_cfg.outPath, CsharpFile.NewBuilder(ToolUtil.ToUpperCamel(fileSimpleName))
                    .AddSpec(nsBuilder.Build())
                    .Build());
            } else {
                foreach (var element in dsFile.EnclosedElements) {
                    if (!element.Kind.IsNamedType()) continue; // inst
                    DSNamedType namedType = (DSNamedType)element;
                    try {
                        TypeSpec.Builder typeBuilder = _helper.Generate(namedType);
                        ToolUtil.WriteToFile(_cfg.outPath, csharpNamespace, typeBuilder.Build());
                    }
                    catch (Exception ex) {
                        throw new Exception($"file: {fileName}, type: {namedType.FullName}", ex);
                    }
                }
            }
        }
    }
}
}