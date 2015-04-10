using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DBFlex {
    class FsPatternGettern : IPatternGetter {
        private const string SearchPattern = "*.sql";

        private string _filePath;
        private Dictionary<string, string> _patterns;
        private FileSystemWatcher _fsWatcher;

        public void SetSource(string src) {
            _filePath = src;
            _patterns = Load(src);

            if (Directory.Exists(src)) {
                _fsWatcher = new FileSystemWatcher(src);

                _fsWatcher.IncludeSubdirectories = true;

                _fsWatcher.Changed += _fsWatcher_Changed;
                _fsWatcher.Created += _fsWatcher_Created;
                _fsWatcher.Deleted += _fsWatcher_Deleted;
                _fsWatcher.Renamed += _fsWatcher_Renamed;

                _fsWatcher.EnableRaisingEvents = true;
            }
        }

        void _fsWatcher_Renamed(object sender, RenamedEventArgs e) {
            _patterns = Load(_filePath);
        }

        void _fsWatcher_Deleted(object sender, FileSystemEventArgs e) {
            _patterns = Load(_filePath);
        }

        void _fsWatcher_Created(object sender, FileSystemEventArgs e) {
            _patterns = Load(_filePath);
        }

        void _fsWatcher_Changed(object sender, FileSystemEventArgs e) {
            _patterns = Load(_filePath);
        }

        private Dictionary<string, string> Load(string dir) {
            if (!Directory.Exists(dir)) return new Dictionary<string, string>();

            var files = Directory.GetFiles(dir, SearchPattern);
            var dirs = Directory.GetDirectories(dir);

            var res = new Dictionary<string, string>();

            foreach (var file in files) {
                if (file.StartsWith("-")) continue; // skip commented files with the first symbol "-"

                var fileConten = File.ReadAllText(file);
                var patternName = Path.GetFileNameWithoutExtension(file);

                res[patternName] = fileConten;
            }

            foreach (var dr in dirs) {
                var patternsFromDir = Load(dr);
                foreach (var ptrn in patternsFromDir) {
                    res[ptrn.Key] = ptrn.Value;
                }
            }


            Console.WriteLine("{1} => {0}", res.Count, dir);

            return res;
        }

        public string GetPattern(string patternName) {
            return _patterns.ContainsKey(patternName) ? _patterns[patternName] : "";
        }
    }
}
