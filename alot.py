import os, sys, subprocess, traceback, random
from enum import Enum
from datetime import datetime, timedelta
from winsound import PlaySound, SND_FILENAME
from colorama import init, Fore


DIR = "dat knowledge" #path to the directory containing knowledge files
GUI = "AlotGUI" + os.sep + "AlotGUI" + os.sep + "bin" + os.sep + "Debug" + os.sep + "AlotGUI.exe" #path to the program used to display images
SOUND_CORRECT = "sounds" + os.sep + "correct.wav" #sounds played after the user answers a question
SOUND_WRONG = "sounds" + os.sep + "wrong.wav"
COLOR_UNLEARNED = Fore.YELLOW #colors used to print questions
COLOR_LEARNED = Fore.GREEN


Type = Enum("Type", "Number Range String Image Diagram Class List")


def parseFile(path):
	with open(path) as f:
		data = eval(f.read()) #using eval instead of the safer ast.literal_eval because ast's version can't parse datetime objects

	#load metadata
	metapath = path.replace(DIR, DIR + os.sep + "!METADATA")
	if os.path.isfile(metapath):
		with open(metapath) as f:
			metadata = eval(f.read())
	else: #first time accessing this knowledge file
		print("New file: " + os.path.basename(path))
		metadata = {}

	changes = False

	#new entries?
	nNew = 0
	for key in data:
		if key not in metadata:
			metadata[key] = {
				"learned": False,
				"step": 1,
				"lastTest": datetime.now(),
				"nextTest": datetime.now() + timedelta(hours=22)
			}

			if type(data[key]) is dict: #each attribute needs its own learning step tracker
				metadata[key]["step"] = {}
				for attribute in data[key]:
					metadata[key]["step"][attribute] = 1
			
			nNew += 1
			changes = True

	#deleted entries?
	toDel = []
	nDel = 0

	for key in metadata:
		if key not in data:
			toDel.append(key)
			nDel += 1
			changes = True

	for key in toDel:
		del metadata[key]

	return data, metadata, changes, nNew, nDel


def saveToFile(data, metadata, path):
	with open(path, "w") as f:
		f.write("{\n\t" + "\n\t".join('{!r}: {!r},'.format(key, value) for (key, value) in data.items()) + "\n}")

	#save metadata
	metapath = path.replace(DIR, DIR + os.sep + "!METADATA")
	with open(metapath, "w") as f:
		f.write(str(metadata).replace("datetime.datetime", "datetime"))


def checkForExit(answer):
	exit = False
	immediately = False

	if "exit" in answer:
		exit = True
		answer = answer.replace("exit", "").replace(", ", "").rstrip()
		if answer == "":
			immediately = True

	return answer, exit, immediately


def removeTypos(userAnswer, originalCorrectAnswer):
	userAnswer = userAnswer.lower()
	correctAnswer = originalCorrectAnswer.lower()

	if userAnswer[:len(correctAnswer)] == correctAnswer or len(userAnswer) < len(correctAnswer) - 1: #if the answer is correct or hopelessly wrong (more than just a typo)
		return userAnswer

	#check for a swapped pair of letters
	if len(userAnswer) >= len(correctAnswer):
		for i in range(len(correctAnswer)-1):
			if userAnswer[:i] + userAnswer[i+1] + userAnswer[i] + userAnswer[i+2:len(correctAnswer)] == correctAnswer:
				print("You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
				return userAnswer[:i] + userAnswer[i+1] + userAnswer[i] + userAnswer[i+2:]

	#check for extra letters
	for i in range(len(correctAnswer)+1):
		if userAnswer[:i] + userAnswer[i+1:len(correctAnswer)+1] == correctAnswer:
			print("You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
			return userAnswer[:i] + userAnswer[i+1:]

	#check for missing letters
	for i in range(len(correctAnswer)):
		if userAnswer[:i] + correctAnswer[i] + userAnswer[i:len(correctAnswer)-1] == correctAnswer:
			print("You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
			return userAnswer[:i] + correctAnswer[i] + userAnswer[i:]

	#check for a mistyped letter
	if len(userAnswer) >= len(correctAnswer):
		for i in range(len(correctAnswer)):
			if userAnswer[:i] == correctAnswer[:i] and userAnswer[i+1:len(correctAnswer)] == correctAnswer[i+1:]:
				print("You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
				return userAnswer[:i] + correctAnswer[i] + userAnswer[i+1:]

	return userAnswer


#returns keys of entries ready for testing from a specific category
def getReadyKeys(metacatalot):
	ready = []

	for key in metacatalot:
		if metacatalot[key]["nextTest"] <= datetime.now():
			ready.append(key)

	return ready


def timeUntilAvailable(metacatalot):
	earliest = datetime.max

	for key in metacatalot:
		if metacatalot[key]["nextTest"] < earliest:
			earliest = metacatalot[key]["nextTest"]

	time = str(earliest - datetime.now())
	if '.' in time: #remove microseconds
		time = time[:time.index('.')]
	return time


#checks if there are any more steps to be learned (based on the type of entry)
def isLearned(step, answer):
	return step == maxSteps(answer) + 1


def maxSteps(answer):
	answerType = getType(answer)

	if answerType is Type.Number or answerType is Type.Range:
		return 4
	elif answerType is Type.String:
		return 5
	elif answerType is Type.Image:
		return 1


def getType(entry):
	entryType = type(entry)

	if entryType is int:
		return Type.Number
	elif entryType is tuple and type(entry[0]) is int:
		return Type.Range
	elif entryType is tuple and type(entry[0]) is str:
		return Type.Diagram
	elif entryType is str and os.path.exists(fullPath(entry)):
		return Type.Image
	elif entryType is str:
		return Type.String
	elif entryType is list:
		return Type.List
	elif entryType is dict:
		return Type.Class


def getMaxKeyLen(dictionary):
	maxLen = 0

	for key in dictionary:
		if len(key) > maxLen:
			maxLen = len(key)

	return str(maxLen + 8)


def toString(answer):
	answerType = getType(answer)

	if answerType is Type.Range:
		return str(answer[0]) + " - " + str(answer[1])
	elif answerType is Type.Class:
		return str(answer).replace('{', '').replace('}', '').replace(", ", "\n   ").replace("'", "")
	else:
		return str(answer)


def listMatchingAttributes(catalot, attribute):
	attribs = []
	for key in catalot:
		if getType(catalot[key]) is Type.Class and attribute in catalot[key]:
			attribs.append(catalot[key][attribute])

	return attribs


#returns a list of dictionary keys whose attribute value is different than parameter value
def listKeysWithUniqueAttribute(catalot, attribute, value):
	keys = list(catalot.keys())
	toDel = []

	for key in keys:
		if getType(catalot[key]) is not Type.Class or attribute not in catalot[key] or catalot[key][attribute] == value:
			toDel.append(key)

	for key in toDel:
		keys.remove(key)

	return keys


def removeParentheses(s):
	while True:
		try:
			lb = s.index('(')
			ub = s.index(')', lb) + 1
			s = s[:lb] + s[ub:]
			s = s.rstrip()
		except:
			return s


def fullPath(relativePath):
	path = DIR + os.sep + "!IMAGES" + os.sep + relativePath

	if os.path.isabs(path):
		return path
	else:
		return os.getcwd() + os.sep + path


def feedback(msg, playSound=True):
	correct = not "Wrong" in msg

	if correct:
		color = Fore.GREEN
	else:
		color = Fore.RED
	colorPrint(msg, color)

	if playSound:
		if correct:
			PlaySound(SOUND_CORRECT, SND_FILENAME)
		else:
			PlaySound(SOUND_WRONG, SND_FILENAME)


def colorPrint(text, color):
	print(color, end="")
	print(text)
	print(Fore.RESET, end="")


def qType_MultipleChoice(q, a, altA, color):
	#altA is a pool of values from which alternate answers are randomly selected
	colorPrint(toString(q) + "\n", color)

	#get other choices
	answers = [a]
	if a in altA:
		altA.remove(a)

	while len(answers) < 6 and len(altA) > 0:
		nextA = random.choice(altA)
		altA.remove(nextA)

		if type(nextA) == type(a) and nextA not in answers:
			answers.insert(random.randint(0, len(answers)), nextA)

	#print choices
	i = 1
	for answer in answers:
		print(str(i) + ". " + toString(answer))
		i += 1

	#wait for user's answer
	choice = ""
	while (choice == "" or not choice.isdigit() or int(choice) < 1 or int(choice) > len(answers)) and "exit" not in choice:
		choice = input("\nChoose the correct answer: ")

	choice, exit, immediately = checkForExit(choice)

	try:
		correct = answers[int(choice)-1] == a
	except:
		correct = False

	if not correct:
		correct = toString(a)

	return correct, exit, immediately


def qType_EnterAnswer(q, a, color):
	colorPrint(toString(q), color)

	#construct hint
	aType = getType(a)

	if aType is Type.Range:
		key = str(a[0]) + " - " + str(a[1])
		hint = '_' * len(str(a[0])) + " - " + '_' * len(str(a[1]))
	else:
		key = str(a)
		hint = ""

		showFirstLetters = aType is Type.String
		firstLetter = True
		insideParentheses = False

		for i in range(len(key)):
			if key[i].isalnum():
				if (showFirstLetters and firstLetter) or insideParentheses:
					hint += key[i]
					firstLetter = False
				else:
					hint += '_'
			elif key[i] == ' ':
				hint += ' '
				firstLetter = True
			else: #punctuation
				hint += key[i]
				firstLetter = True
				if key[i] == '(':
					insideParentheses = True
				elif key[i] == ')':
					insideParentheses = False

	#wait for user's answer
	answer, exit, immediately = checkForExit(input("> " + hint + "\n> "))

	#ignore segments in parentheses
	answer = removeParentheses(answer)
	correctAnswer = removeParentheses(toString(a))

	#ignore punctuation
	answer = ''.join(e for e in answer.lower() if e.isalnum())
	correctAnswer = ''.join(e for e in correctAnswer.lower() if e.isalnum())

	#check answer
	if getType(a) is Type.String:
		answer = removeTypos(answer, correctAnswer)

	correct = answer == correctAnswer
	if not correct:
		correct = toString(a)

	return correct, exit, immediately


def qType_FillString(q, s, difficulty, corewords, color):
	#split string into parts
	parts = s.replace("\n", " __NEWLINE__ ").split()

	for i in range(len(parts)):
		if parts[i] == "__NEWLINE__":
			parts[i] = "\n"

	#split punctuations into separate parts
	words = []  #doesn't include 1-letter words
	noSpaceAfterThisPart = []

	i = 0
	while i < len(parts):
		onlyPunctuation = True
		for char in parts[i]:
			if char.isalnum():
				onlyPunctuation = False
				break

		if not onlyPunctuation and len(parts[i]) > 1:
			j = 1
			if not parts[i][0].isalnum(): #if part starts with a punctuation mark
				#find how many punctuation marks it starts with
				while j < len(parts[i]) and not parts[i][j].isalnum(): #check if there is more than one punctuation character
					j += 1
			else:
				#find the first punctuation mark
				while j < len(parts[i]) and parts[i][j].isalnum():
					j += 1

			if j < len(parts[i]):
				#keep the segment before the punctuation in this part
				parts.insert(i+1, parts[i][j:])
				parts[i] = parts[i][:j]
				noSpaceAfterThisPart.append(i)
		i += 1

	#no space after '\n'
	for i in range(len(parts)):
		if parts[i] == '\n':
			noSpaceAfterThisPart.append(i)

	#choose which parts to make blank
	#ignore punctuation parts, 1-letter parts, and corewords
	insideParentheses = False

	for i in range(len(parts)):
		if parts[i][0].isalnum() and len(parts[i]) > 1 and parts[i].lower() not in corewords and not insideParentheses:
			words.append(i)

		for char in parts[i]:
			if char == '(':
				insideParentheses = True
			elif char == ')':
				insideParentheses = False

	if difficulty == 1:
		nBlanks = 1
	elif difficulty == 2:
		nBlanks = max(len(words) // 2, 1)
	else:
		nBlanks = len(words)

	blanks = []
	for i in range(nBlanks):
		nextIndex = random.choice(words)
		words.remove(nextIndex)
		blanks.append(nextIndex)

	#print the string with blanks
	colorPrint(q + ":", color)

	for i in range(len(parts)):
		if i not in blanks:
			print(parts[i], end="")
		else:
			print(parts[i][0] + "_" * (len(parts[i])-1), end="")

		if i not in noSpaceAfterThisPart:
			print(" ", end="")
	print("\n\nFill in the blanks:")

	#keep asking the user to fill the blank
	allCorrect = True
	nPrevChars = 0
	extraAnswerChars = ""

	for i in range(len(parts)):
		nChars = len(parts[i])
		nPrevChars += nChars + 1

		extraAnswerChars = extraAnswerChars.lstrip()
		if extraAnswerChars != "":
			#the user previously entered more than one blank, so check if the rest of his answer is correct
			extraAnswerChars = removeTypos(extraAnswerChars, parts[i])

			if extraAnswerChars[:nChars].lower() == parts[i].lower():
				extraAnswerChars = extraAnswerChars[nChars:]

				if i not in noSpaceAfterThisPart and i < len(parts)-1 and extraAnswerChars != "": #if this is not the last part, the next letter must be a space
					if extraAnswerChars[0] == ' ':
						extraAnswerChars = extraAnswerChars[1:]
					else:
						allCorrect = False
						break
				continue
			else:
				extraAnswerChars = ""

		if i not in blanks:
			print(parts[i], end="")
			if i not in noSpaceAfterThisPart:
				print(" ", end="")
		else:
			answer, exit, immediately = checkForExit(input(""))
			answer = removeTypos(answer, parts[i])

			if answer[:nChars].lower() != parts[i].lower():
				allCorrect = False
				break
			else:
				extraAnswerChars = answer[nChars:].rstrip() #if the user entered more than one blank entry, save the rest of his answer

			if exit:
				allCorrect = i == max(blanks) #if this was the last blank part then the answer is allCorrect
				break

			print(" " * (nPrevChars + len(extraAnswerChars) + 1), end="") #align text
	print("\n")

	if not allCorrect:
		allCorrect = s

	return allCorrect, exit, immediately


def qType_RecognizeList(listKey, items, color):
	#pick 3 random items
	randomItems = list(items)
	random.shuffle(randomItems)
	randomItems = randomItems[:min(3, len(randomItems))]

	for item in randomItems:
		print(item)

	answer, exit, immediately = checkForExit(input("What list do these items belong to? "))

	if answer.lower() == listKey.lower():
		return True, exit, immediately
	else:
		return listKey, exit, immediately


def qType_RecognizeItem(listKey, items, color):
	colorPrint(listKey, color)
	index = random.randint(0, len(items)-1)

	if random.randint(0, 3) == 0:
		answer, exit, immediately = checkForExit(input("What is the index of this item: " + toString(items[index]) + "? "))

		try:
			if int(answer)-1 == index:
				return True, exit, immediately
			else:
				return str(index+1), exit, immediately
		except:
			return str(index+1), exit, immediately
	else:
		answer, exit, immediately = checkForExit(input("What is the {}. item in this list? ".format(index+1)))

		if answer.lower() == toString(items[index]).lower():
			return True, exit, immediately
		else:
			return toString(items[index]), exit, immediately


def qType_OrderItems(listKey, items, color):
	colorPrint(listKey, color)

	#shuffle items
	shuffledItems = list(items)
	shuffledIndices = list([i+1 for i in range(len(items))])

	for i in range(len(items)):
		index = random.randint(i, len(shuffledItems)-1)
		#correctOrder.append(shuffledIndices[index])

		shuffledItems[i], shuffledItems[index] = shuffledItems[index], shuffledItems[i]
		shuffledIndices[i], shuffledIndices[index] = shuffledIndices[index], shuffledIndices[i]

	correctOrder = ""
	for i in range(len(items)):
		correctOrder += " " + str(shuffledIndices.index(i+1) + 1)
	correctOrder = correctOrder[1:]

	#get answer from user
	for i in range(len(shuffledItems)):
		print("{}. {}".format(i+1, removeParentheses(shuffledItems[i])))

	answer, exit, immediately = checkForExit(input("Enter the correct order of these items: "))

	#cleanup answer
	answer = answer.replace('.', '').replace(',', ' ')
	while "  " in answer:
		answer = answer.replace("  ", " ")

	if answer == correctOrder:
		return True, exit, immediately
	else:
		return correctOrder, exit, immediately


def qType_Image(imageKey, path, color):
	gui = subprocess.Popen(GUI + ' "{}"'.format(path))
	answer, quit, immediately = checkForExit(input("What is this image associated with? "))
	gui.terminate()

	if answer.lower() == imageKey.lower():
		return True, quit, immediately
	else:
		return imageKey, quit, immediately


def quizNumber(catalot, key, step, color,  attribute=""):
	if step == 1:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(key + ", " + attribute, catalot[key][attribute], listMatchingAttributes(catalot, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(key, catalot[key], list(catalot.values()), color)
	elif step == 2:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(attribute + ", " + toString(catalot[key][attribute]), key, listKeysWithUniqueAttribute(catalot, attribute, catalot[key][attribute]), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot[key], key, list(catalot.keys()), color)
	elif step == 3:
		if attribute != "":
			correct, exit, immediately = qType_EnterAnswer(key + ", " + attribute, catalot[key][attribute], color)
		else:
			correct, exit, immediately = qType_EnterAnswer(key, catalot[key], color)
	elif step == 4:
		if attribute != "":
			correct, exit, immediately = qType_EnterAnswer(key + ", " + attribute, catalot[key][attribute], color)
		else:
			correct, exit, immediately = qType_EnterAnswer(toString(catalot[key]), key, color)

	return correct, exit, immediately


def quizString(catalot, key, step, corewords, color, attribute=""):
	if step == 1:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(key + ", " + attribute, catalot[key][attribute], listMatchingAttributes(catalot, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(key, catalot[key], list(catalot.values()), color)
	elif step == 2:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(attribute + ", " + toString(catalot[key][attribute]), key, listKeysWithUniqueAttribute(catalot, attribute, catalot[key][attribute]), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot[key], key, list(catalot.keys()), color)
	elif step == 3:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 1, corewords, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 1, corewords, color)
	elif step == 4:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 2, corewords, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 2, corewords, color)
	elif step == 5:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 3, corewords, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 3, corewords, color)

	return correct, exit, immediately


def quizList(listKey, items, step, learned=False):
	if learned:
		color = COLOR_LEARNED
	else:
		color = COLOR_UNLEARNED

	colorPrint(listKey + ":", color)

	if step <= len(items):
		for i in range(1, step):
			print("{0}. {1}".format(i, items[i-1]))
		finalStep = False 
	else:
		#this is the final step, which demands the user to enter every list item
		finalStep = True
		step = 1

	#print("Enter the next item in the list:")
	correct = True

	while type(correct) is bool and step <= len(items):
		correct, exit, immediately = qType_EnterAnswer("{}. item".format(step), toString(items[step-1]), color)

		if immediately:
			break
		elif learned: #if testing learned entry, only one list item is needed
			return correct, exit, immediately
		elif type(correct) is bool:
			step += 1
		else:
			feedback("Wrong! Correct answer: " + correct, False)

	if not finalStep:
		return step, exit, immediately
	elif step == len(items) + 1:
		return True, exit, immediately
	else:
		return "False", exit, immediately #returning "False" instead of False because the script uses the type of that variable to check if correct, not the value


def quiz(category, catalot, metacatalot, corewords):	
	ready = getReadyKeys(metacatalot)
	if len(ready) == 0:
		return
	nCorrect = 0
	nTested = 0

	print("Category: " + category)

	while len(ready) > 0:
		#prepare next question
		print("\n\n")

		key = random.choice(ready)
		entry = catalot[key]
		entryType = getType(entry)
		meta = metacatalot[key]
		step = meta["step"]

		if not meta["learned"]:
			color = COLOR_UNLEARNED
			if entryType is Type.Number or entryType is Type.Range:
				correct, exit, immediately = quizNumber(catalot, key, step, color)
			elif entryType is Type.Diagram:
				gui = subprocess.Popen(GUI + ' "{}"'.format(fullPath(entry[0])))
				correct, exit, immediately = quizList(key, entry[1], step)
				gui.terminate()
			elif entryType is Type.Image:
				correct, exit, immediately = qType_Image(key, fullPath(entry), color)
			elif entryType is Type.String:
				correct, exit, immediately = quizString(catalot, key, step, corewords, color)
			elif entryType is Type.Class:
				#custom class: ask a question for each attribute
				correct = {}
				for attribute in entry:
					attributeType = getType(entry[attribute])
					if isLearned(step[attribute], entry[attribute]):
						correct[attribute] = "already learned"
						exit = False
					elif attributeType is Type.Number or attributeType is Type.Range:
						correct[attribute], exit, immediately = quizNumber(catalot, key, step[attribute], color, attribute)
					elif attributeType is Type.Diagram:
						gui = subprocess.Popen(GUI + ' "{}"'.format(entry[attribute][0]))
						correct[attribute], exit, immediately = quizList(key, entry[attribute][1], step[attribute])
						gui.terminate()
					elif attributeType is Type.Image:
						correct[attribute], exit, immediately = qType_Image(key, fullPath(entry[attribute]), color)
					elif attributeType is Type.String:
						correct[attribute], exit, immediately = quizString(catalot, key, step[attribute], corewords, color, attribute)
					elif attributeType is Type.List:
						correct[attribute], exit, immediately = quizList(key + ", " + attribute, entry[attribute], step[attribute])

					if exit:
						break
			elif entryType is Type.List:
				correct, exit, immediately = quizList(key, entry, step)
		else:
			color = COLOR_LEARNED
			if entryType is Type.Number or entryType is Type.Range:
				correct, exit, immediately = quizNumber(catalot, key, random.randint(1, 4), color)
			elif entryType is Type.Diagram:
				gui = subprocess.Popen(GUI + ' "{}"'.format(fullPath(entry[0])))
				if random.randint(0, 1) == 0:
					correct, exit, immediately = quizList(key, random.randint(1, len(entry[1])), step)
				else:
					correct, exit, immediately = qType_RecognizeItem(key, entry[1], color)
				gui.terminate()
			elif entryType is Type.Image:
				correct, exit, immediately = qType_Image(key, fullPath(entry), color)
			elif entryType is Type.String:
				correct, exit, immediately = quizString(catalot, key, random.randint(1, 5), corewords, color)
			elif entryType is Type.Class:
				attribute = random.choice(list(entry.keys()))
				attributeType = getType(entry[attribute])

				if attributeType is Type.Number or attributeType is Type.Range:
					correct, exit, immediately = quizNumber(catalot, key, random.randint(1, 4), color, attribute)
				elif attributeType is Type.String:
					correct, exit, immediately = quizString(catalot, key, random.randint(1, 5), corewords, color, attribute)
			elif entryType is Type.List:
				qType = random.choice([quizList, qType_RecognizeList, qType_RecognizeItem, qType_OrderItems])

				if qType is quizList:	
					correct, exit, immediately = qType(key, entry, random.randint(1, len(entry)), True)
				else:
					correct, exit, immediately = qType(key, entry, color)

		if not immediately:
			#log result
			#the variable correct will be bool if the user entered the right answer. If he failed, correct will be a string holding the actual correct answer
			#for classes correct will be a dictionary containing this representation (bool or string) for every attribute
			if type(correct) is dict:
				#class entry
				if not meta["learned"]:
					#advance any attributes that have been correctly answered
					allLearned = True
					anyMistakes = False
					maxW = getMaxKeyLen(correct)

					for attribute in correct:
						if correct[attribute] == "already learned":
							print(("{:<" + maxW + "}Already learned").format(attribute))
						elif type(correct[attribute]) is bool:
							feedback(("{:<" + maxW + "}Correct!").format(attribute))
							meta["step"][attribute] += 1
							allLearned = allLearned and isLearned(meta["step"][attribute], entry[attribute])
						else:
							feedback(("{0:<" + maxW + "}Wrong! Correct answer: {1}").format(attribute, correct[attribute]))
							allLearned = False
							anyMistakes = True

					if not anyMistakes:
						nCorrect += 1

					#check if this was the final step to learn the entry
					if allLearned:
						feedback("Entry learned!")
						meta["learned"] = True
						meta["nextTest"] = datetime.now() + timedelta(days=6, hours=22)
					else:
						meta["nextTest"] = datetime.now() + timedelta(hours=22)
				else:
					if type(correct) is bool:
						feedback("Correct!")
						nCorrect += 1
						meta["nextTest"] = datetime.now() + (meta["nextTest"] - meta["lastTest"]) + timedelta(days=6, hours=22)
					else:
						feedback("Wrong! Entry unlearned! Correct answer: " + correct)
						meta["learned"] = False
						meta["nextTest"] = datetime.now() + timedelta(hours=22)

						for attribute in meta["step"]:
							meta["step"][attribute] = 1
			elif type(correct) is int:
				#quizList returns correct as the new step
				newStep = correct
				if newStep > meta["step"]:
					correct = True
					nCorrect += 1
				else:
					correct = False

				if entryType is Type.List:
					print("List progress @ {}%.".format(100*(newStep-1)//len(entry)))
				else:
					print("Diagram progress @ {}%.".format(100*(newStep-1)//len(entry[1])))
				
				if correct:
					PlaySound(SOUND_CORRECT, SND_FILENAME)
				else:
					PlaySound(SOUND_WRONG, SND_FILENAME)

				meta["step"] = newStep
				meta["nextTest"] = datetime.now() + timedelta(hours=22)
			else:
				#standard entry type (int, tuple, str), or quizList that returned True or "False"
				if not meta["learned"]:
					if type(correct) is bool:
						feedback("Correct!")
						nCorrect += 1

						if entryType is Type.List or entryType is Type.Diagram:
							feedback("Entry learned!")
							meta["learned"] = True
							meta["nextTest"] = datetime.now() + timedelta(days=6, hours=22)
						else:
							meta["step"] += 1
							
							if isLearned(meta["step"], entry):
								feedback("Entry learned!")
								meta["learned"] = True
								meta["nextTest"] = datetime.now() + timedelta(days=6, hours=22)
							else:
								print("Entry progress @ {}%.".format(100*meta["step"]//maxSteps(entry)))
								meta["nextTest"] = datetime.now() + timedelta(hours=22)
					else:
						if correct is not "False": #if it is "False" then quizList has already printed the correct answer
							feedback("Wrong! Correct answer: " + correct)
						print("Entry progress @ {}%.".format(100*(meta["step"]-1)//maxSteps(entry)))
						meta["nextTest"] = datetime.now() + timedelta(hours=22)
				else:
					if type(correct) is bool:
						feedback("Correct!")
						nCorrect += 1
						meta["nextTest"] = datetime.now() + (meta["nextTest"] - meta["lastTest"]) + timedelta(days=6, hours=22)
					else:
						feedback("Wrong! Entry unlearned! Correct answer: " + correct)
						meta["learned"] = False
						meta["step"] = 1
						meta["nextTest"] = datetime.now() + timedelta(hours=22)

			meta["lastTest"] = datetime.now()
			nTested += 1

		ready.remove(key)
		if exit:
			break

	if len(ready) == 0:
		print("\nNo more entries. ", end="")
	if nTested > 0:
		print("Score: {0} / {1} ({2}%)".format(nCorrect, nTested, 100*nCorrect//nTested))
	print("\n")

	return nTested > 0


def mainLoop(alot, metalot, changes):
	#load corewords
	if os.path.isfile("corewords.txt"):
		with open("corewords.txt") as f:
			corewords = f.read().split()
	else:
		corewords = ["BibleThump", "FeelsBadMan"]

	choice = ""
	while choice != "exit":
		maxLen = getMaxKeyLen(alot)
		
		print(("\n\t{0:<" + maxLen + "}Ready entries").format("File"))
		total = 0
		i = 0
		cats = {}
		for category in alot:
			n = len(getReadyKeys(metalot[category]))
			if n > 0:
				total += n
				i += 1
				cats[i] = category
				print(("{0:<8}{1:<" + maxLen + "}{2}").format(str(i)+'.', category, n))
			else:
				print(("\t{0:<" + maxLen + "}Available in {1}").format(category, timeUntilAvailable(metalot[category])))

		if total > 0:
			i += 1
			cats[i] = "all"
			cats[i+1] = "exit"
			print(("{0:<8}{1:<" + maxLen + "}{2}").format(str(i)+'.', "all", total))
			print(("{0:<8}{1:<" + maxLen + "}\n").format(str(i+1)+'.', "exit"))

			choice = ""
			while choice not in alot and not (choice.isdigit() and int(choice) in cats.keys()) and choice != "all" and choice != "exit":
				choice = input('Choose a category: ')

			if choice.isdigit():
				choice = cats[int(choice)]

			if choice == "all": #test all categories one by one
				for category in alot:
					changes[category] = quiz(category, alot[category], metalot[category], corewords)
					
					if len(getReadyKeys(metalot[category])) > 0:
						break
			elif choice != "exit":
				changes[choice] = quiz(choice, alot[choice], metalot[choice], corewords)
		else:
			print("\nNo entries ready for testing.")
			return False

	return True


#MAIN
print("Alot of Knowlege v0.6")

init() #colorama init

#ensure required directories exist
if not os.path.isdir(DIR):
	os.makedirs(DIR)
if not os.path.isdir(DIR + os.sep + "!METADATA"):
	os.makedirs(DIR + os.sep + "!METADATA")
if not os.path.isdir(DIR + os.sep + "!IMAGES"):
	os.makedirs(DIR + os.sep + "!IMAGES")

#just in case the paths have redundant '\' at the end
DIR = os.path.normpath(DIR)

#load knowledge
alot = {}
metalot = {}
changes = {}

for filename in os.listdir(DIR):
	if ".txt" in filename:
		category = os.path.splitext(filename)[0]
		alot[category], metalot[category], changes[category], nNew, nDel = parseFile(DIR + os.sep + filename)

		if nNew > 0 or nDel > 0:
			print(filename + " changed: ", end="")
			if nNew > 0:
				print(str(nNew) + " new entries", end="")
				if nDel > 0:
					print(", ", end="")
				else:
					print("\n")
			if nDel > 0:
				print(str(nDel) + " deletions")

#show "main menu"
try:
	immediately = mainLoop(alot, metalot, changes)
except:
    print("Uh-oh: " + str(traceback.format_exception(*sys.exc_info())))
    immediately = False
    
finally: #save changes
	for category in alot:
		if changes[category]:
			saveToFile(alot[category], metalot[category], DIR + os.sep + category + ".txt")

if not immediately:
	print("Press Enter to exit...")
	input()