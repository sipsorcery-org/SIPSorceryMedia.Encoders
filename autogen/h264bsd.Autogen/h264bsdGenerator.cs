using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace h264bsd.AutoGen
{
    public class h264bsdGenerator : ILibrary
    {
        //public static string[] _requiredDefines = {
        //    "VPX_CODEC_ABI_VERSION",
        //    "VPX_ENCODER_ABI_VERSION",
        //    "VPX_DECODER_ABI_VERSION",
        //    "VPX_DL_REALTIME",
        //    "VPX_EFLAG_FORCE_KF",
        //    "VPX_FRAME_IS_KEY"
        //};

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            //foreach(var tu in ctx.TranslationUnits)
            //{
            //    foreach (var macro in tu.PreprocessedEntities.OfType<MacroDefinition>().Where(x => _requiredDefines.Any(y => y == x.Name)))
            //    {
            //        Console.WriteLine($"{macro.Name}->{macro.Expression}");

            //        // Do Something?
            //    }
            //}
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            //throw new NotImplementedException();
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            var module = options.AddModule("h264bsd");
            module.IncludeDirs.Add(@"C:\Dev\github\h264bsd\src");
            module.Headers.Add("h264bsd_decoder.h");
            module.LibraryDirs.Add(@"C:\Dev\github\h264bsd\win\bin\Debug\x64");
            module.Libraries.Add("h264bsd.dll");
            module.Libraries.Add("h264bsd.lib");
        }

        public void SetupPasses(Driver driver)
        {
            //throw new NotImplementedException();
        }
    }
}
