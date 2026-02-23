import os
from PIL import Image

# ===== SETTINGS =====
input_folder = "input_pngs"
output_folder = "output_pngs"

# color to remove (R, G, B)
REMOVE_COLOR = (180, 180, 180)   # example: magenta background

# tolerance allows removing similar colors
TOLERANCE = 0  # 0 = exact match, higher = more forgiving
# ====================


def color_match(c1, c2, tolerance):
    return all(abs(a - b) <= tolerance for a, b in zip(c1, c2))


def process_image(path, save_path):
    img = Image.open(path).convert("RGBA")
    pixels = img.load()

    width, height = img.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]

            if color_match((r, g, b), REMOVE_COLOR, TOLERANCE):
                pixels[x, y] = (0, 0, 0, 0)  # make transparent

    # Crop to non-transparent area
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)

    img.save(save_path)
    print(f"Processed: {os.path.basename(path)}")


def main():
    os.makedirs(output_folder, exist_ok=True)

    for file in os.listdir(input_folder):
        if file.lower().endswith(".png"):
            input_path = os.path.join(input_folder, file)
            output_path = os.path.join(output_folder, file)
            process_image(input_path, output_path)


if __name__ == "__main__":
    main()
