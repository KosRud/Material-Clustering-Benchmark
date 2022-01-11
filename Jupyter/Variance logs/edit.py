import os

subdir = "1. subsampling"
for fname in os.listdir(f"./{subdir}"):
    if os.path.isfile(f"./{subdir}/{fname}"):
        content = ""
        with open(f"./{subdir}/{fname}", mode="r") as file:
            content = file.readlines()
        newContent = [content[0]]
        print(newContent)
        for line in content[1:]:
            if line == "Frame,Variance\n":
                break
            newContent.append(line)
        with open(f"./{subdir}/{fname}", mode="w") as file:
            file.writelines(newContent)
