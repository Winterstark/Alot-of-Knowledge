import os, os.path, sys

DIR = "dat knowledge" #folder containing the knowledge files

if len(sys.argv) < 3:
	print("Not enough arguments!")
else:
	category = sys.argv[1]
	if ".txt" in category:
		category = category.replace(".txt", "")
	filePath = DIR + os.sep + category + ".txt"

	newEntry = sys.argv[2]
	for i in range(3, len(sys.argv)):
		newEntry += " " + sys.argv[i]

	if ':' not in newEntry or newEntry.index(':') == 0 or newEntry.index(':') == len(newEntry)-1:
		print("New entry needs to be in the form of <key>: <value>!")
	else:
		delimiters = ['}', ']', '"', "'"]
		index = newEntry.index(':')
		if newEntry[index+1] != ' ':
			newEntry = newEntry[:index+1] + ' ' + newEntry[index+1:]

		if newEntry[0] not in delimiters:
			newEntry = "'{}':{}".format(newEntry[:index], newEntry[index+1:])
			index = newEntry.index(':')
		if newEntry[-1] not in delimiters:
			newEntry = "{}: '{}'".format(newEntry[:index], newEntry[index+2:])

		index = newEntry.find("'")
		while index != -1:
			if index > 1 and index != len(newEntry)-1 and newEntry[index+1] != ':' and newEntry[index-2:index] != ": ":
				newEntry = newEntry[:index] + '\\' + newEntry[index:]
			index = newEntry.find("'", index + 2)

		if not os.path.isfile(filePath):
			print("Target knowledge file doesn't exist!")
		else:
			with open(filePath) as f:
				contents = f.read()

			index = contents.rfind('}')

			if index == -1:
				print("Unable to process target knowledge file.")
			else:
				contents = contents[:index] + '\t' + newEntry + ",\n}"

				with open(filePath, 'w') as f:
					f.write(contents)

				print("Successfully added new entry to " + category + "\nNew entry: " + newEntry)

input("\nPress Enter to exit...")