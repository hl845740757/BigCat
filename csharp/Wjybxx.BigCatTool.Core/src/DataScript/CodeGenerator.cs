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
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// csharp代码生成器
/// </summary>
public class CodeGenerator
{
    private static readonly AttributeSpec processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(CodeGenerator));

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
    /// <param name="helper">生成器辅助类</param>
    public CodeGenerator(DSRepository repository, CodeGeneratorCfg cfg, ICollection<string> fileNames, CodeGeneratorHelper? helper = null) {
        _repository = repository;
        _cfg = cfg;
        _helper = helper ?? new CodeGeneratorHelper(cfg, processorInfo);
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
                    DsonObject<string> options = DSUtil.GetOptions(namedType);
                    if (Annotation.GetBool(options, DSAnnotations.KEY_NON_GENERATE)) {
                        continue;
                    }
                    if (nsBuilder.nestedSpecs.Count > 0) {
                        nsBuilder.AddSpec(new CodeBlockSpec(CodeBlock.NewLine));
                    }
                    try {
                        TypeSpec.Builder typeBuilder = _helper.Generate(namedType);
                        nsBuilder.AddSpec(typeBuilder.Build());
                    }
                    catch (Exception ex) {
                        throw new Exception($"file: {fileName}, type: {namedType.FullName}", ex);
                    }
                }
                GeneratorUtil.WriteToFile(_cfg.outPath, CsharpFile.NewBuilder(ToolUtil.ToUpperCamel(fileSimpleName))
                    .AddSpec(nsBuilder.Build())
                    .Build());
            } else {
                foreach (var element in dsFile.EnclosedElements) {
                    if (!element.Kind.IsNamedType()) continue; // inst
                    DSNamedType namedType = (DSNamedType)element;
                    DsonObject<string> options = DSUtil.GetOptions(namedType);
                    if (Annotation.GetBool(options, DSAnnotations.KEY_NON_GENERATE)) {
                        continue;
                    }
                    try {
                        TypeSpec.Builder typeBuilder = _helper.Generate(namedType);
                        GeneratorUtil.WriteToFile(_cfg.outPath, csharpNamespace, typeBuilder.Build());
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