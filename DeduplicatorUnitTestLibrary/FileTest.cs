using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Deduplicator.Common;
using System.Threading.Tasks;

namespace DeduplicatorUnitTestLibrary
{
    [TestClass]
    public class FileTest
    {
        File fileLow = new File("AName","APath", ".xaml",
                                new DateTime(2017, 11, 5, 10, 10, 10),
                                new DateTime(2017, 11, 5, 10, 10, 10),
                                1000, false, false);
        File fileEqual1 = new File("BName", "BPath", ".yaml",
                                new DateTime(2018, 11, 5, 10, 10, 10),
                                new DateTime(2018, 11, 5, 10, 10, 10),
                                2000, false, false);
        File fileEqual2 = new File("BName", "BPath", ".yaml",
                                new DateTime(2018, 11, 5, 10, 10, 10),
                                new DateTime(2018, 11, 5, 10, 10, 10),
                                2000, false, false);
        File fileHight = new File("CName", "CPath", ".zaml",
                                new DateTime(2019, 11, 5, 10, 10, 10),
                                new DateTime(2019, 11, 5, 10, 10, 10),
                                3000, false, false);
        [TestMethod]
        public async Task CompareTo_FileEqualByName()
        {
            int i = await fileEqual1.CompareTo(fileEqual2, FileAttribs.Name);
             Assert.AreEqual(0, i);
        }

        [TestMethod]
        public async Task CompareTo_FileEqualByPath()
        {
            int i = await fileEqual1.CompareTo(fileEqual2, FileAttribs.Path);
            Assert.AreEqual(0, i);
        }
    }
}
