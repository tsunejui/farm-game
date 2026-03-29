import os
import sys
from PIL import Image
import yaml

def png_to_pixel_yaml(png_path, output_path, name):
    """
    Converts a PNG image to a pixel-art YAML definition.
    """
    img = Image.open(png_path).convert("RGBA")
    width, height = img.size
    
    # 1. Collect unique colors and build a local palette
    # Use a set of characters for keys
    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?/"
    color_to_char = {}
    char_to_hex = {}
    char_index = 0
    
    # We use '.' for fully transparent pixels
    pixel_matrix = []
    
    for y in range(height):
        row = ""
        for x in range(width):
            r, g, b, a = img.getpixel((x, y))
            
            if a < 128:
                row += "."
            else:
                # RGB to Hex
                hex_color = f"#{r:02x}{g:02x}{b:02x}"
                if hex_color not in color_to_char:
                    if char_index < len(chars):
                        c = chars[char_index]
                        color_to_char[hex_color] = c
                        char_to_hex[c] = hex_color
                        char_index += 1
                    else:
                        # Fallback if too many colors (unlikely for pixel art)
                        c = "?" 
                
                row += color_to_char.get(hex_color, "?")
        pixel_matrix.append(row)

    # 2. Build the YAML structure
    yaml_data = {
        "metadata": {
            "name": name
        },
        "type": "png",
        "output_dir": "Images/extracted_trees",
        "palette": char_to_hex,
        "data": [
            "\n" + "\n".join(pixel_matrix) + "\n"
        ]
    }

    # 3. Write YAML
    # We use a custom dumper to handle the multi-line string properly
    class LiteralDumper(yaml.SafeDumper):
        def represent_data(self, data):
            if isinstance(data, str) and "\n" in data:
                return self.represent_scalar('tag:yaml.org,002:str', data, style='|')
            return super().represent_data(data)

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w") as f:
        yaml.dump(yaml_data, f, Dumper=LiteralDumper, sort_keys=False, allow_unicode=True)

    print(f"Generated YAML for {name}")

def process_extracted_trees(source_dir, target_dir):
    if not os.path.exists(source_dir):
        print(f"Source directory {source_dir} not found.")
        return
        
    files = [f for f in os.listdir(source_dir) if f.endswith(".png")]
    files.sort()
    
    for filename in files:
        name = os.path.splitext(filename)[0]
        png_path = os.path.join(source_dir, filename)
        yaml_path = os.path.join(target_dir, f"{name}.yaml")
        png_to_pixel_yaml(png_path, yaml_path, name)

if __name__ == "__main__":
    src = "example/images/tree/extracted"
    tge = "example/images/tree/yaml"
    process_extracted_trees(src, tge)
