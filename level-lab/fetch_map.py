import urllib.request

url = "https://maps.speccy.cz/maps/Rebelstar2.png"
out_path = "level-lab/rebelstar2_reference_map.png"

try:
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req) as resp, open(out_path, 'wb') as f:
        data = resp.read()
        f.write(data)
    print(f"Downloaded map to {out_path} ({len(data)} bytes)")
    # Read PNG header: width and height are at offset 16 and 20 (4 bytes big-endian)
    w = int.from_bytes(data[16:20], 'big')
    h = int.from_bytes(data[20:24], 'big')
    print(f"PNG dimensions: {w} x {h}")
except Exception as e:
    print(f"Error downloading: {e}")
