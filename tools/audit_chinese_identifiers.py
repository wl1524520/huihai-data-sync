"""List Chinese C# identifiers, excluding comments, literals and build output."""
from pathlib import Path
import re
from collections import defaultdict
import sys

ROOT = Path(__file__).resolve().parents[1]
TOKENS = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[^\W\d]\w*')

def identifiers(text):
    for match in TOKENS.finditer(text):
        token = match.group()
        if re.match(r'[^\W\d]', token) and re.search(r'[\u3400-\u9fff]', token):
            yield match

if __name__ == '__main__':
    names = defaultdict(list)
    for path in sorted(ROOT.rglob('*.cs')):
        if {'bin', 'obj', 'tools'} & set(path.relative_to(ROOT).parts):
            continue
        text = path.read_text(encoding='utf-8-sig')
        for match in identifiers(text):
            location = '{}:{}'.format(path.relative_to(ROOT), text.count('\n', 0, match.start()) + 1)
            names[match.group()].append(location)
    for name, locations in sorted(names.items()):
        print('{} | {} | {}'.format(name, len(locations), locations[0]))
    print('Unique identifiers: {}; occurrences: {}'.format(len(names), sum(map(len, names.values()))))
    if '--check' in sys.argv and names:
        sys.exit(1)
