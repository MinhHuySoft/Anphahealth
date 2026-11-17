"""Archive all discoverable public Anpha HTML/content/media and Chemlabs page inventory.
Run: python -X utf8 scripts/crawl.py. No form submissions or private endpoints.
"""
import concurrent.futures as futures
import hashlib
import json
import re
import time
from collections import deque
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urljoin, urlsplit, urlunsplit, unquote
from urllib.robotparser import RobotFileParser
import requests
from bs4 import BeautifulSoup

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'RECON/raw'
ASSETS = ROOT / 'src/AnphaHealth.Web/wwwroot/media'
RAW.mkdir(parents=True, exist_ok=True)
ASSETS.mkdir(parents=True, exist_ok=True)
BASE = 'https://anphahealth.vn/'
HEADERS = {'User-Agent': 'AnphaHealth-UI-Research/1.0 (public-content archive)'}
session = requests.Session()
session.headers.update(HEADERS)
errors, pages, assets, visited = [], [], set(), set()
robot = RobotFileParser()
robot.parse((RAW / 'robots.txt').read_text().splitlines())

def normalized(u):
    p = urlsplit(urljoin(BASE, u))
    if p.hostname not in ('anphahealth.vn', 'www.anphahealth.vn'): return None
    if p.query or re.search(r'/wp-(admin|json|content|includes)/|/feed/?$|/xmlrpc', p.path): return None
    if re.search(r'\.(png|jpg|jpeg|gif|svg|webp|pdf|zip|docx?|xlsx?|mp4)$', p.path, re.I): return None
    return urlunsplit(('https', 'anphahealth.vn', p.path or '/', '', ''))

def get(u):
    for attempt in range(3):
        try:
            r = session.get(u, timeout=35)
            if r.status_code >= 500 and attempt < 2: time.sleep(1); continue
            return r
        except requests.RequestException as ex:
            if attempt == 2: return ex
    return None

queue = deque([BASE])
discovery = {}
for endpoint in ['wp-sitemap.xml', 'sitemap_index.xml', 'wp-json/wp/v2/pages?per_page=100', 'wp-json/wp/v2/posts?per_page=100', 'wp-json/wp/v2/media?per_page=100']:
    r = get(BASE + endpoint)
    if isinstance(r, requests.Response):
        discovery[endpoint] = {'status': r.status_code, 'bytes': len(r.content)}
        (RAW / ('discovery-' + endpoint.split('?')[0].replace('/', '_'))).write_bytes(r.content)
        if r.ok and 'json' in r.headers.get('Content-Type', ''):
            for page_number in range(1, int(r.headers.get('X-WP-TotalPages', '1')) + 1):
                result = r if page_number == 1 else get(BASE + endpoint + '&page=' + str(page_number))
                if isinstance(result, requests.Response) and result.ok:
                    data = result.json()
                    (RAW / (endpoint.split('?')[0].replace('/', '_') + f'-{page_number}.json')).write_text(json.dumps(data, ensure_ascii=False), encoding='utf8')
                    for item in data:
                        if u := normalized(item.get('link', '')): queue.append(u)
                        if item.get('source_url'): assets.add(item['source_url'])
        if r.ok and 'xml' in r.headers.get('Content-Type', ''):
            xml = BeautifulSoup(r.content, 'xml') if False else BeautifulSoup(r.content, 'html.parser')
            for loc in xml.select('loc'):
                u = loc.get_text(strip=True)
                if u.endswith('.xml'):
                    sr = get(u)
                    if isinstance(sr, requests.Response) and sr.ok:
                        for inner in BeautifulSoup(sr.content, 'html.parser').select('loc'):
                            if n := normalized(inner.get_text(strip=True)): queue.append(n)
                elif n := normalized(u): queue.append(n)

def parse(u):
    r = get(u)
    if not isinstance(r, requests.Response): return None, [], {'url': u, 'error': str(r)}
    if not r.ok: return None, [], {'url': u, 'status': r.status_code}
    if 'html' not in r.headers.get('Content-Type', ''): return None, [], None
    slug = unquote(urlsplit(u).path).strip('/') or 'home'
    raw_name = slug.replace('/', '__') + '.html'
    (RAW / raw_name).write_bytes(r.content)
    s = BeautifulSoup(r.content, 'html.parser')
    content = s.select_one('.story') or s.select_one('.entry-content') or s.select_one('#wrapper-content') or s.body
    for x in content.select('script,style,form,noscript,iframe,.social-share,.post-meta,.pagination,.comments-area,.comment-respond,nav,header,footer'):
        x.decompose()
    image_urls = set()
    for x in s.select('img[src],a[href]'):
        value = x.get('src') if x.name == 'img' else x.get('href')
        full = urljoin(u, value or '')
        if urlsplit(full).hostname == 'anphahealth.vn' and '/wp-content/uploads/' in full:
            image_urls.add(full.replace('http://', 'https://'))
    for match in re.findall(r'url\([\'\"]?([^\)\'\"]+)', r.text):
        full = urljoin(u, match)
        if '/wp-content/uploads/' in full: image_urls.add(full.replace('http://', 'https://'))
    for x in content.find_all(True):
        for attr in list(x.attrs):
            if attr.startswith('on') or attr in ['style', 'class', 'id', 'srcset', 'sizes', 'width', 'height']: del x[attr]
    title_node = s.select_one('h1') or s.select_one('.section-title h3')
    title = title_node.get_text(' ', strip=True) if title_node else (s.title.get_text(' ', strip=True).split(' – ')[0] if s.title else slug)
    text = content.get_text(' ', strip=True)
    links = [n for x in s.select('a[href]') if (n := normalized(urljoin(u, x['href'])))]
    return {'url': u, 'slug': slug, 'title': title, 'rawFile': 'RECON/raw/' + raw_name,
            'html': str(content), 'text': text, 'images': sorted(image_urls),
            'headings': [x.get_text(' ',strip=True) for x in content.select('h1,h2,h3')],
            'links': sorted(set(links))}, links, None

with futures.ThreadPoolExecutor(max_workers=4) as pool:
    while queue:
        batch = []
        while queue and len(batch) < 4:
            u = queue.popleft()
            if u in visited: continue
            visited.add(u)
            if robot.can_fetch(HEADERS['User-Agent'], u): batch.append(u)
        for page, links, err in pool.map(parse, batch):
            if err: errors.append(err)
            if page:
                pages.append(page)
                assets.update(page['images'])
                queue.extend(x for x in links if x not in visited)
        if batch: print(f'Crawled {len(pages)} pages / discovered {len(visited)+len(queue)}', flush=True)
        if len(visited) > 2000:
            errors.append({'error': 'Safety limit reached; crawl incomplete', 'remaining': len(queue)})
            break

def asset(u):
    r = get(u)
    if not isinstance(r, requests.Response) or not r.ok:
        return {'url':u, 'error': str(r) if not isinstance(r, requests.Response) else r.status_code}
    suffix = Path(urlsplit(u).path).suffix.lower() or '.bin'
    filename = hashlib.sha256(u.encode()).hexdigest()[:12] + '-' + unquote(Path(urlsplit(u).path).name)
    filename = re.sub(r'[^a-zA-Z0-9._-]', '-', filename)
    (ASSETS / filename).write_bytes(r.content)
    return {'url':u, 'local':'/media/'+filename, 'bytes':len(r.content), 'type':r.headers.get('Content-Type','')}

with futures.ThreadPoolExecutor(max_workers=5) as pool: manifest = list(pool.map(asset, sorted(assets)))
mapping = {x['url']:x['local'] for x in manifest if 'local' in x}
for page in pages:
    for original, local in mapping.items():
        page['html'] = page['html'].replace(original, local).replace(original.replace('https:', 'http:'), local)
    page['localImages'] = [mapping[x] for x in page['images'] if x in mapping and re.search(r'\.(png|jpg|jpeg|gif|webp)$', mapping[x],re.I)]
    # Keep public content links within the Blazor content archive.
    s = BeautifulSoup(page['html'], 'html.parser')
    for a in s.select('a[href]'):
        if n := normalized(a['href']):
            a['href'] = '/noi-dung/' + (urlsplit(n).path.strip('/') or 'home')
        elif a['href'].startswith('javascript:'): del a['href']
    page['html'] = str(s)

(ROOT/'data/anpha-content.json').write_text(json.dumps(sorted(pages,key=lambda x:x['slug']),ensure_ascii=False,indent=2),encoding='utf8')
(ROOT/'data/asset-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
report={'source':BASE,'crawledAt':datetime.now(timezone.utc).isoformat(),'pageCount':len(pages),'assetCount':len(mapping),'assetErrors':[x for x in manifest if 'error' in x], 'discovery':discovery,'attempted':len(visited),'remaining':len(queue),'errors':errors,'routes':[{'url':p['url'],'title':p['title'],'slug':p['slug']} for p in pages]}
(ROOT/'RECON/routes/anpha-route-map.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')

template_base='https://7oroof.com/demos/chemlabs/'
template_queue=deque(['index.html']); template_seen=set(); templates=[]
while template_queue:
    name=template_queue.popleft()
    if name in template_seen: continue
    template_seen.add(name)
    r=get(template_base+name)
    if not isinstance(r, requests.Response) or not r.ok:
        templates.append({'file':name,'status':r.status_code if isinstance(r,requests.Response) else str(r)});continue
    (RAW/('chemlabs-'+name)).write_bytes(r.content)
    s=BeautifulSoup(r.content,'html.parser')
    templates.append({'file':name,'status':r.status_code,'title':s.title.get_text(' ',strip=True) if s.title else '', 'headings':[x.get_text(' ',strip=True) for x in s.select('h1,h2,h3')][:20]})
    for a in s.select('a[href]'):
        h=a['href'].split('#')[0]
        if re.fullmatch(r'[a-zA-Z0-9-]+\.html',h) and h not in template_seen:template_queue.append(h)
(ROOT/'RECON/routes/chemlabs-route-map.json').write_text(json.dumps(templates,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({'pages':len(pages),'assets':len(mapping),'errors':errors,'templates':len(templates)},ensure_ascii=False))
