from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).parent
source = root / 'life-stages-concept-refined.png'
target = root / 'life-stages-concept-final.png'
image = Image.open(source).convert('RGB')
pixels = np.asarray(image, dtype=np.float32).copy()
height, width = pixels.shape[:2]
# Tone only the three lower-row characters; retain paper grain and soft edges.
for column, shift in enumerate(((-74, -59, -27), (-61, -53, -44), (-29, -17, 0))):
    left, right = round(column * width / 3), round((column + 1) * width / 3)
    region = pixels[round(height * .48):, left:right]
    luminance = region @ np.array([.2126, .7152, .0722])
    weight = np.clip((224 - luminance) / 49, 0, 1)
    weight = weight * weight * (3 - 2 * weight)
    region += weight[..., None] * np.array(shift)
Image.fromarray(np.clip(pixels, 0, 255).astype(np.uint8)).save(target)
print(target)
