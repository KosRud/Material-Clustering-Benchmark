import os


def main():
    for dir_name in os.listdir("."):
        if os.path.isdir(dir_name) is False:
            continue
        for fname in os.listdir(f"./{dir_name}"):
            print(fname)
            *name, extension = fname.split(".")
            name = "".join(name)
            if extension != "csv":
                continue
            settings = {
                name: value
                for name, value in [
                    setting.split(":")
                    for setting in name.split("|")
                ]
            }

            if "mode" in settings:
                settings["algorithm"] = settings["mode"]
                del settings["mode"]

            new_name = "".join(
                [
                    f"{setting_name}:{settings[setting_name]}|"
                    for setting_name in settings
                ]
            )
            new_name = new_name[:-1]
            os.rename(
                os.path.join(dir_name, fname),
                os.path.join(dir_name, new_name+".csv")
            )


if __name__ == '__main__':
    main()

_ = """
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
"""
