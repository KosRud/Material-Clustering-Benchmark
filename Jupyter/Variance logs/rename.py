import os

for file_name in os.listdir("./"):
    new_file_name = file_name.replace("@", "|").replace("=", ":")
    os.rename(file_name, new_file_name)
