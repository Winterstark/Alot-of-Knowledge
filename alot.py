import os, sys, subprocess, traceback, random, struct, calendar, re
from enum import Enum
from datetime import datetime, timedelta
from time import sleep
from winsound import PlaySound, SND_FILENAME
from colorama import init, Fore


DIR = "dat knowledge" #path to the directory containing knowledge files
GUI = "AlotGUI" + os.sep + "AlotGUI" + os.sep + "bin" + os.sep + "Debug" + os.sep + "AlotGUI.exe" #path to the program used to display images
SOUND_CORRECT = "sounds" + os.sep + "correct.wav" #sounds played after the user answers a question
SOUND_WRONG = "sounds" + os.sep + "wrong.wav"
SOUND_LEARNED = "sounds" + os.sep + "learned.wav"
COLOR_UNLEARNED = Fore.YELLOW #colors used to print questions
COLOR_LEARNED = Fore.GREEN


Type = Enum("Type", "Number NumberRange Date DateRange String Image Sound Diagram Class List Set Tuple Geo")



#a custom class instead of DateTime allows for greater flexibility, e.g. 11 November 1918, 322 BC, 5th century, 3rd millennium BC, 60s
class Date:
	ZERO = datetime(1, 1, 1)
	PREFIXES = [("early ", "Early "), ("mid ", "Mid "), ("late ", "Late "), ("1. half ", "First half of the "), ("2. half ", "Second half of the ")]

	def __init__(self, value):
		#init fields
		self.bc = self.isDecade = False
		self.prefix = ""
		self.M = self.c = self.y = self.m = self.d = -1

		if value == '--':
			#set Date to today
			self.y = datetime.now().year
			self.m = datetime.now().month
			self.d = datetime.now().day
		else:
			if value[0] == '-': #b.c. year
				value = value[1:]
				self.bc = True

			if value[-2:] == "m.":
				self.prefix, value = Date.extractPrefix(value)
				self.M = int(value[:-2])
			elif value[-2:] == "c.":
				self.prefix, value = Date.extractPrefix(value)
				self.c = int(value[:-2])
			elif value[-1] == 's':
				self.prefix, value = Date.extractPrefix(value[:-1])
				self.isDecade = True
				self.y = int(value)
			else:
				parts = value.split('-')
				if len(parts) == 1:
					self.y = int(parts[0])
				elif len(parts) == 2:
					self.y = int(parts[0])
					self.m = int(parts[1])
				else: #== 3
					self.y = int(parts[0])
					self.m = int(parts[1])
					self.d = int(parts[2])


	def __str__(self):
		if self.M != -1:
			s = self.prefix + Date.convertToOrdinal(self.M) + " millennium"
		elif self.c != -1:
			s = self.prefix + Date.convertToOrdinal(self.c) + " century"
		else:
			s = str(self.y)

			if self.m != -1:
				s = Date.fullMonthName(self.m) + " " + s
			if self.d != -1:
				s = str(self.d) + " " + s
			if self.isDecade:
				s += 's'

		if self.bc:
			s += " BC"

		return s


	def __repr__(self):
		if self.isToday():
			s = "--"
		else:
			if self.prefix == "":
				outputPrefix = ""
			else:
				#convert prefix back to short form
				for prefix in Date.PREFIXES:
					if prefix[1] == self.prefix:
						outputPrefix = prefix[0]
						break

			if self.M != -1:
				s = outputPrefix + str(self.M) + "m."
			elif self.c != -1:
				s = outputPrefix + str(self.c) + "c."
			else:
				s = str(self.y)
				if self.m != -1:
					s += "-" + str(self.m)
				if self.d != -1:
					s += "-" + str(self.d)
				if self.isDecade:
					s = outputPrefix + s + 's'

			if self.bc:
				s = "-" + s

		return "'" + s + "'"


	def __eq__(self, other):
		if isinstance(other, self.__class__):
			return self.__dict__ == other.__dict__
		else:
			return False


	def __hash__(self):
		return hash(str(self))


	def __lt__(self, other):
		if getType(other) is Type.Date:
			return self.totalDays() < other.totalDays()
		elif getType(other) is Type.DateRange:
			return self.totalDays() < (other[0].totalDays() + other[1].totalDays()) / 2
		else:
			return toString(self) < toString(other)


	def __gt__(self, other):
		return not self < other and self != other


	def isToday(self):
		return not self.bc and self.y == datetime.now().year and self.m == datetime.now().month and self.d == datetime.now().day


	def totalDays(self):
		if self.M != -1:
			year = self.c * 1000
			month = 6
			day = 15
		elif self.c != -1:
			year = self.c * 100
			month = 6
			day = 15
		else:
			year = self.y
			if self.m != -1:
				month = self.m
			else:
				month = 6
			if self.d != -1:
				day = self.d
			else:
				day = 15

		if self.bc:
			return -((year-1)*365 + (month-1)*30 + day) #datetime doesn't support BC dates, so this is an estimation
		else:
			return (datetime(year, month, day) - Date.ZERO).days


	def dayDifference(self, days):
		return abs(self.totalDays() - days)


	def precision(self):
		if self.M != -1:
			return "M"
		elif self.c != -1:
			return "c"
		elif self.isDecade:
			return "dec"
		elif self.d != -1:
			return "d"
		elif self.m != -1:
			return "m"
		else:
			return "y"


	def precisionPrompt(self):
		precision = self.precision()
		prefixed = self.prefix != ""

		if precision == "M":
			if not prefixed:
				return "Millennium"
			else:
				return "Part of Millennium"
		elif precision == "c":
			if not prefixed:
				return "Century"
			else:
				return "Part of Century"
		elif precision == "dec":
			return "Decade"
		elif precision == "y":
			return "Year"
		elif precision == "m":
			return "Year-Month"
		elif precision == "d":
			return "Year-Month-Day"


	def toGUIFormat(self, partOfRange):
		precision = self.precision()

		if precision == "M" or precision == "c":
			if precision == "M":
				baseDate = self.M * 1000
				scale = 1000
			else:
				baseDate = self.c * 100
				scale = 100

			if self.bc:
				baseDate *= -1
			else:
				baseDate += -scale + 1

			if self.prefix == 'Early ':
				period = int(1/3 * scale)
			elif self.prefix == 'Mid ':
				baseDate += int(1/3 * scale)
				period = int(1/3 * scale)
			elif self.prefix == 'Late ':
				baseDate += int(2/3 * scale)
				period = int(1/3 * scale)
			elif self.prefix == "First half of the ":
				period = int(1/2 * scale)
			elif self.prefix == "Second half of the ":
				baseDate += int(1/2 * scale)
				period = int(1/2 * scale)
			else:
				period = int(1 * scale)

			if partOfRange:
				return baseDate + period // 2 #midpoint
			else:
				return "{} - {}".format(baseDate, baseDate + period) #range
		else:
			if calendar.isleap(self.y):
				totalDaysInYear = 366
			else:
				totalDaysInYear = 365

			y = self.y
			if self.bc:
				y *= -1

			if self.m == -1:
				return y
			else:
				if self.d == -1:
					return str(y + datetime(y, self.m, 1).timetuple().tm_yday / totalDaysInYear)
				else:
					return str(y + datetime(y, self.m, self.d).timetuple().tm_yday / totalDaysInYear)


	def isAlmostCorrect(self, answer):
		if getType(answer) is Type.Date:
			answerDate = answer
		elif Date.isValid(answer):
			answerDate = Date(answer)
		else:
			return False

		precision = self.precision()
		if precision == "M":
			if self.M < 10:
				marginForError = 1
			else:
				marginForError = 2
			return abs(self.c - answerDate.c) <= marginForError
		elif precision == "c":
			if not self.bc:
				marginForError = 1
			elif self.c <= 20:
				marginForError = 2
			else:
				marginForError = 4
			return abs(self.c - answerDate.c) <= marginForError
		elif precision == "dec":
			return abs(self.y - answerDate.y) <= 10
		else:
			marginForError = (datetime.now().year - self.y) // 100 + 1
			return abs(self.y - answerDate.y) <= marginForError


	#checks if string represents a Date (short form, e.g. "18c.")
	def isValid(entry):
		if entry == "":
			return False

		if type(entry) is str:
			if entry == "--":
				return True
			elif entry[0] == '-':
				entry = entry[1:]

			prefix, entry = Date.extractPrefix(entry)

			if entry[-2:] == "c." or entry[-2:] == "m.":
				try:
					int(entry[:-2])
					return True
				except:
					pass
			elif entry[-1] == 's':
				try:
					int(entry[:-1])
					return True
				except:
					pass

			try:
				parts = entry.split('-')
				if len(parts) > 3:
					return False

				y = int(parts[0])
				if y == 0:
					return False
				
				if len(parts) > 1:
					m = int(parts[1])

					if len(parts) > 2:
						d = int(parts[2])
					else:
						d = 1

					datetime(2015, m, d) #check if month/day combination is valid

				return True
			except:
				return False
		else:
			return False


	def fullMonthName(m):
		return datetime(1, int(m), 1).strftime("%B")


	def convertToOrdinal(num):
		s = str(num)

		if s[-1] == '1' and (len(s) == 1 or s[-2] != '1'):
			s += "st"
		elif s[-1] == '2' and (len(s) == 1 or s[-2] != '1'):
			s += "nd"
		elif s[-1] == '3' and (len(s) == 1 or s[-2] != '1'):
			s += "rd"
		else:
			s += "th"

		return s


	def extractPrefix(entry):
		for prefix in Date.PREFIXES:
			if prefix[0] in entry:
				return prefix[1], entry.replace(prefix[0], "")

		return "", entry #no prefix found



def msgGUI(msg):
	pipe.write(struct.pack('I', len(msg)) + bytes(msg, "UTF-8"))
	pipe.seek(0)


def getFeedbackFromGUI(catalot={}):
	while "pipe" not in locals():
		try:
			pipe = open(r'\\.\pipe\alotPipeFeedback', 'r+b', 0)
		except:
			#give gui time to start the pipe
			sleep(0.2)

	pipe.seek(0)
	n = struct.unpack('B', pipe.read(1))[0]

	correct = n == 1
	if not correct:
		correct = "FalseGEO"
		userSelection = pipe.read(n).decode("utf-8")

		if userSelection != "":
			if catalot != {}:
				#find the key corresponding to this geoName
				geoKey = userSelection #if the search fails, try with the given geoName

				for key in catalot:
					if getType(catalot[key]) is Type.Geo:
						name = catalot[key]
						if '/' in name:
							name = name[name.rfind('/')+1:] #strip "GEO:type/" from the name
						if name == userSelection:
							geoKey = key
							break
					elif getType(catalot[key]) is Type.Class:
						for attribute in catalot[key]:
							if getType(catalot[key][attribute]) is Type.Geo:
								name = catalot[key][attribute]
								if '/' in name:
									name = name[name.rfind('/')+1:] #strip "GEO:type/" from the name
								if name == userSelection:
									geoKey = key
									break

				userSelection = geoKey

			correct += " You selected -> " + userSelection

	return correct, False, False


def addNodeToFamilyTree(catalot, key, visited=[]):
	if key not in catalot or type(catalot[key]) is not dict:
		return ""
	else:
		nodeOutput = ""
		checkNodes = []
		visited.append(key)

		if "Appearance" in catalot[key]:
			nodeOutput += key + ", Appearance: " + fullPath(catalot[key]["Appearance"]) + "\n"

		if "Parents" in catalot[key]:
			parents = ""
			for node in catalot[key]["Parents"]:
				parents += node + '/'
				checkNodes.append(node)

			nodeOutput += key + ", Parents: " + parents[:-1] + "\n"
		if "Consort" in catalot[key] and catalot[key]["Consort"] not in visited:
			checkNodes.append(catalot[key]["Consort"])
			nodeOutput += key + ", Consort: " + str(catalot[key]["Consort"]) + "\n"

		for k in catalot:
			if type(catalot[k]) is dict and "Parents" in catalot[k] and key in catalot[k]["Parents"] and k not in visited:
				checkNodes.append(k)

		for node in checkNodes:
			if node not in visited and type(node) is str:
				nodeOutput += addNodeToFamilyTree(catalot, node)

		return nodeOutput


def exportFamilyTree(catalot, key, isQuestion):
	if isQuestion:
		return key + " ?\n" + addNodeToFamilyTree(catalot, key)
	else:
		return key + "\n" + addNodeToFamilyTree(catalot, key)


def collectDatesForTimeline(entryKey, data, img, timeline):
	compoundTypes = [Type.Class, Type.List, Type.Tuple]
	dataType = getType(data)

	if dataType is Type.Class:
		if "Picture" in data:
			img = fullPath(data["Picture"])
		elif "Appearance" in data:
			img = fullPath(data["Appearance"])

		for key in data:
			entryType = getType(data[key])
			if entryType is Type.Date or entryType is Type.DateRange:
				if entryKey == "":
					timeline[key] = [data[key], entryKey, img]
				else:
					timeline[entryKey] = [data[key], entryKey, img]
			elif entryType in compoundTypes:
				if entryKey == "":
					collectDatesForTimeline(key, data[key], img, timeline)
				else:
					collectDatesForTimeline(entryKey, data[key], img, timeline)
	elif dataType is Type.List or dataType is Type.Tuple:
		for i in range(len(data)):
			entryType = getType(data[i])
			if entryType is Type.Date or entryType is Type.DateRange:
				if i > 0:
					timeline[data[i-1]] = [data[i], entryKey, img]
				else:
					timeline[entryKey] = [data[i], entryKey, img]
			elif entryType in compoundTypes:
				collectDatesForTimeline(entryKey, data[i], img, timeline)


def exportTimelineForGUI(alot):
	#save Dates and Ranges to a file
	timeline = {}
	for category in alot:
		collectDatesForTimeline("", alot[category], "", timeline)

	with open("timeline.txt", "w") as f:
		for key in timeline:
			if getType(timeline[key][0]) is Type.Date:
				f.write("{} :: {} // {} // {}\n".format(key, timeline[key][0].toGUIFormat(False), timeline[key][1], timeline[key][2]).replace("'", "").replace('"', ''))
			else:
				f.write("{} :: {} - {} // {} // {}\n".format(key, timeline[key][0][0].toGUIFormat(True), timeline[key][0][1].toGUIFormat(True), timeline[key][1], timeline[key][2]).replace("'", "").replace('"', ''))


def convertToDateIfAnyInTuple(tpl):
	valuesList = list(tpl)
	containsDate = False

	for i in range(len(valuesList)):
		if Date.isValid(valuesList[i]):
			valuesList[i] = Date(valuesList[i])
			containsDate = True
		elif getType(valuesList[i]) is Type.DateRange:
			valuesList[i] = (Date(valuesList[i][0]), Date(valuesList[i][1]))
			containsDate = True
		elif getType(valuesList[i]) is Type.Tuple:
			newI = convertToDateIfAnyInTuple(valuesList[i])
			if newI != i:
				valuesList[i] = newI
				containsDate = True

	if containsDate:
		return tuple(valuesList)
	else:	
		return tpl


def convertToDateIfAny(data):
	compoundTypes = [Type.Class, Type.List, Type.Set]

	if getType(data) is Type.Class:
		for key in data:
			if Date.isValid(data[key]):
				data[key] = Date(data[key])
			elif getType(data[key]) is Type.DateRange:
				data[key] = (Date(data[key][0]), Date(data[key][1]))
			elif getType(data[key]) is Type.Tuple:
				data[key] = convertToDateIfAnyInTuple(data[key])
			elif getType(data[key]) in compoundTypes:
				convertToDateIfAny(data[key])
	elif getType(data) is Type.List:
		for i in range(len(data)):
			if Date.isValid(data[i]):
				data[i] = Date(data[i])
			elif getType(data[i]) is Type.DateRange:
				data[i] = (Date(data[i][0]), Date(data[i][1]))
			elif getType(data[i]) is Type.Tuple:
				data[i] = convertToDateIfAnyInTuple(data[i])
			elif getType(data[i]) in compoundTypes:
				convertToDateIfAny(data[i])
	elif getType(data) is Type.Set:
		toDel = []
		toAdd = []

		for value in data:
			if Date.isValid(value):
				toDel.append(value)
				toAdd.append(Date(value))
			elif getType(value) is Type.DateRange:
				toDel.append(value)
				toAdd.append((Date(value[0]), Date(value[1])))
			elif getType(value) is Type.Tuple:
				newValue = convertToDateIfAnyInTuple(value)
				if newValue != value:
					toDel.append(value)
					toAdd.append(newValue)
			elif getType(value) in compoundTypes:
				convertToDateIfAny(value)

		for value in toDel:
			data.remove(value)
		for value in toAdd:
			data.add(value)


def parseFile(path):
	with open(path) as f:
		contents = f.read()
		data = eval(contents) #using eval instead of the safer ast.literal_eval because ast's version can't parse datetime objects

		#ensure there are no multiple entries with the same key (prevent the user from overwriting an existing entry)
		for key in data:
			if len(re.findall("\t['\"]{}['\"]: ".format(re.escape(key)), contents)) > 1:
				print("Two entries with the same key ({}) detected while parsing {}!".format(key, os.path.basename(path)))
				sys.exit(0)

	convertToDateIfAny(data) #convert date representations to Date objects

	#load metadata
	metapath = path.replace(DIR, DIR + os.sep + "!METADATA")
	if os.path.isfile(metapath):
		with open(metapath) as f:
			metadata = eval(f.read())
	else: #first time accessing this knowledge file
		print("New file: " + os.path.basename(path))
		metadata = {}

	#new entries?
	nNew = 0
	nExp = 0
	nRed = 0

	for key in data:
		if key not in metadata:
			#init metadata entry
			metadata[key] = {
				"learned": False,
				"step": 1,
				"lastTest": datetime.now(),
				"nextTest": datetime.now() + timedelta(hours=22)
			}

			if getType(data[key]) is Type.Class: #each attribute needs its own learning step tracker
				metadata[key]["step"] = {}
				for attribute in data[key]:
					metadata[key]["step"][attribute] = 1
			
			nNew += 1
		elif type(data[key]) is dict:
			#check if entry has been expanded
			hasExpanded = False
			if type(metadata[key]["step"]) is not dict:
				metadata[key]["step"] = {}
				hasExpanded = True
			else:
				#check if attributes have been removed
				toDel = []
				for attribute in metadata[key]["step"]:
					if attribute not in data[key]:
						toDel.append(attribute)
						nRed += 1

				for attribute in toDel:
					del metadata[key]["step"][attribute]

			for attribute in data[key]:
				if attribute not in metadata[key]["step"]:
					metadata[key]["step"][attribute] = 1

					if metadata[key]["learned"]:
						metadata[key]["learned"] = False
						metadata[key]["nextTest"] = datetime.now() + timedelta(hours=22)

					hasExpanded = True

			if hasExpanded:
				nExp += 1

	#deleted entries?
	toDel = []
	nDel = 0

	for key in metadata:
		if key not in data:
			toDel.append(key)
			nDel += 1

	for key in toDel:
		del metadata[key]

	return data, metadata, nNew, nExp, nRed, nDel


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


def removeTypos(userAnswer, correctAnswer, originalCorrectAnswer="", indentLevel=0, returnTruncatedAnswer=True):
	if originalCorrectAnswer == "":
		originalCorrectAnswer = correctAnswer

	userAnswer = userAnswer.lower()
	correctAnswer = correctAnswer.lower()

	if len(correctAnswer) == 1:
		return userAnswer #don't check for typos with 1-letter answers

	if userAnswer[:len(correctAnswer)] == correctAnswer or len(userAnswer) < len(correctAnswer) - 1: #if the answer is correct or hopelessly wrong (more than just a typo)
		if len(userAnswer) == len(correctAnswer) + 1: #check for an extra letter at the end of user's answer
			print('\t'*indentLevel + "You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
			return userAnswer[:len(correctAnswer)]
		else:
			return userAnswer

	#check for a swapped pair of letters
	if len(userAnswer) >= len(correctAnswer):
		for i in range(len(correctAnswer)-1):
			if userAnswer[:i] + userAnswer[i+1] + userAnswer[i] + userAnswer[i+2:len(correctAnswer)] == correctAnswer:
				print('\t'*indentLevel + "You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
				correctedAnswer = userAnswer[:i] + userAnswer[i+1] + userAnswer[i] + userAnswer[i+2:]
				if returnTruncatedAnswer:
					return correctedAnswer[:len(correctAnswer)]
				else:
					return correctedAnswer
	
	#check for extra letters
	for i in range(len(correctAnswer)+1):
		if userAnswer[:i] + userAnswer[i+1:len(correctAnswer)+1] == correctAnswer:
			print('\t'*indentLevel + "You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
			correctedAnswer = userAnswer[:i] + userAnswer[i+1:]
			if returnTruncatedAnswer:
				return correctedAnswer[:len(correctAnswer)]
			else:
				return correctedAnswer
	
	#check for a mistyped letter
	if len(userAnswer) >= len(correctAnswer):
		for i in range(len(correctAnswer)):
			#don't accept mistyped numbers as typos
			if not correctAnswer[i].isdigit() and userAnswer[:i] == correctAnswer[:i] and userAnswer[i+1:len(correctAnswer)] == correctAnswer[i+1:]:
				print('\t'*indentLevel + "You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
				correctedAnswer = userAnswer[:i] + correctAnswer[i] + userAnswer[i+1:]
				if returnTruncatedAnswer:
					return correctedAnswer[:len(correctAnswer)]
				else:
					return correctedAnswer
	
	#check for missing letters
	if len(userAnswer) + 1 >= len(correctAnswer):
		for i in range(len(correctAnswer)):
			if userAnswer[:i] + correctAnswer[i] + userAnswer[i:len(correctAnswer)-1] == correctAnswer:
				if correctAnswer[i].isalnum(): #if the typo is a missing period or comma, ignore it completely
					print('\t'*indentLevel + "You have a typo in your answer, but it will be accepted anyway. Correct answer:", originalCorrectAnswer)
				correctedAnswer = userAnswer[:i] + correctAnswer[i] + userAnswer[i:]
				if returnTruncatedAnswer:
					return correctedAnswer[:len(correctAnswer)]
				else:
					return correctedAnswer
	
	return userAnswer


def areWordsSynonyms(word1, word2):
	if word1 == "" or word2 == "":
		return False

	word1 = word1.lower()
	word2 = word2.lower()

	if word2 in bSearchThesaurus(word1, 0, len(thesaurus)-1):
		return True
	else:
		return word1 in bSearchThesaurus(word2, 0, len(thesaurus)-1)


def bSearchThesaurus(word, lb, ub):
	if ub - lb <= 10:
		#switch to linear search
		for i in range(lb, ub+1):
			if thesaurus[i][:thesaurus[i].index(',')] == word:
				return thesaurus[i][thesaurus[i].index(',')+1:].split(',')
		return ""

	mid = (lb + ub) // 2
	if thesaurus[mid] == word:
		return thesaurus[mid]
	elif thesaurus[mid] > word:
		return bSearchThesaurus(word, lb, mid-1)
	else:
		return bSearchThesaurus(word, mid+1, ub)


def pluralizeIfNecessary(n, s):
	if abs(n) != 1:
		if s[-1] != 's':
			if s[-1] == 'y' and len(s) >= 2 and s[-2] not in ['a', 'e', 'i', 'o', 'u']:
				s = s[:-1] + "ies"
			else:
				s += 's'
		else:
			s += 'es'

	return "{0} {1}".format(n, s)


#returns keys of entries ready for testing from a specific category, as well as the number of ready new and learned entries
def getReadyKeys(metacatalot):
	readyKeys = []
	nNew = 0
	nLearned = 0

	for key in metacatalot:
		if metacatalot[key]["nextTest"] <= datetime.now():
			readyKeys.append(key)

			if metacatalot[key]["learned"]:
				nLearned += 1
			else:
				nNew += 1

	return readyKeys, nNew, nLearned


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

	if answerType is Type.Number or answerType is Type.NumberRange or answerType is Type.Date or answerType is Type.DateRange or answerType is Type.Geo:
		return 4
	elif answerType is Type.String:
		return 5
	elif answerType is Type.Image or answerType is Type.Sound:
		return 1
	elif answerType is Type.Set:
		return 2
	elif answerType is Type.List:
		return len(answer) + 1
	else:
		print("TYPE NOT SUPPORTED FOR MAXSTEPS():", answerType)


def getType(entry, attribute=""):
	if attribute != "":
		entry = entry[attribute]
	entryType = type(entry)

	if entryType is int:
		return Type.Number
	elif entryType is Date:
		return Type.Date
	elif entryType is tuple:
		if type(entry[1]) is list:
			return Type.Diagram
		elif len(entry) == 2 and (type(entry[0]) is type(entry[1]) is int):
			return Type.NumberRange
		elif len(entry) == 2 and (type(entry[0]) is type(entry[1]) is Date):
			return Type.DateRange
		else:
			return Type.Tuple
	elif entryType is str:
		if entry[:4] == "GEO:":
			return Type.Geo
		elif os.path.exists(fullPath(entry)):
			if pathHasAudioExtension(entry):
				return Type.Sound
			else:
				return Type.Image
		else:
			return Type.String
	elif entryType is list:
		return Type.List
	elif entryType is dict:
		return Type.Class
	elif entryType is set or entryType is frozenset:
		return Type.Set
	else:
		print("UNRECOGNIZED ENTRY TYPE:", entryType)


def pathHasAudioExtension(filePath):
	return os.path.splitext(filePath)[1].lower() in [".ogg", ".mp3", ".wav"]


def getMaxKeyLen(dictionary):
	maxLen = 0

	for key in dictionary:
		if len(key) > maxLen:
			maxLen = len(key)

	return str(maxLen + 8)


def toString(answer, makeMoreReadable=True, hideDates=False):
	answerType = getType(answer)

	if answerType is Type.Number and makeMoreReadable:
		return makeNumberMoreReadable(answer)
	elif answerType is Type.NumberRange:
		return makeNumberMoreReadable(answer[0]) + " - " + makeNumberMoreReadable(answer[1])
	elif answerType is Type.Date:
		if makeMoreReadable:
			return str(answer)
		else:
			return repr(answer)
	elif answerType is Type.DateRange:
		if makeMoreReadable:
			#return the Date range without any redundant data, e.g. "May - June 1940" when the year is the same
			if answer[0].precision() == 'm':
				if answer[0].y == answer[1].y:
					return Date.fullMonthName(answer[0].m) + " - " + Date.fullMonthName(answer[1].m) + " " + str(answer[0].y)
			elif answer[0].precision() == 'd':
				if answer[0].y == answer[1].y:
					if answer[0].m == answer[1].m:
						return str(answer[0].d) + " - " + str(answer[1].d) + " " + Date.fullMonthName(answer[0].m) + " " + str(answer[0].y)
					else:
						return str(answer[0].d) + " " + Date.fullMonthName(answer[0].m) + " - " + str(answer[1].d) + " " + Date.fullMonthName(answer[1].m) + " " + str(answer[0].y)

			#if the Date range is ongoing, replace today's date with '--'
			if answer[1].isToday():
				return str(answer[0]) + "--"
			else:
				return str(answer[0]) + " - " + str(answer[1])
		else:
			return repr(answer[0]) + " - " + repr(answer[1])
	elif answerType is Type.Class:
		return str(answer).replace('{', '').replace('}', '').replace(", ", "\n   ").replace("'", "").replace('"', '')
	elif answerType is Type.Set:
		s = str(answer).replace('frozenset({', '').replace('})', '').replace('{', '').replace('}', '').replace("'", "").replace('"', '')
		if s != "" and s[0] == '(' and s[-1] == ')': #remove brackets only if they enclose the whole string
			#check that the two brackets correspond to each other
			ind = 0
			parenthesesDepth = 1
			while parenthesesDepth > 0 and ind < len(s):
				ind += 1
				if s[ind] == '(':
					parenthesesDepth += 1
				elif s[ind] == ')':
					parenthesesDepth -= 1

			if ind == len(s)-1:
				s = s[1:-1].strip()
		return s
	elif answerType is Type.Tuple:
		s = ""
		for el in answer:
			if getType(el) is not Type.Image and getType(el) is not Type.Sound and not hideDates or (getType(el) is not Type.Date and getType(el) is not Type.DateRange):
				s += toString(el) + ", "
		return s[:-2]
	elif answerType is Type.List:
		return str(answer).replace('[', '').replace(']', '').replace("'", "").replace('"', '')
	else:
		return str(answer)


def makeNumberMoreReadable(num):
	s = str(num)

	if s[-12:] == "000000000000":
		return s[:-12] + " trillion"
	elif s[-9:] == "000000000":
		return s[:-9] + " billion"
	elif s[-6:] == "000000":
		return s[:-6] + " million"
	else:
		#insert a space every 3 digits
		for i in range(len(s)-3, 0, -3):
			s = s[:i] + ' ' + s[i:]

		return s


def isAcceptableAltAnswer(catalot, answers, targetKey, key, attribute):
	if key != targetKey and catalot[key] != catalot[targetKey] and catalot[key] not in answers.values() and getType(catalot[key]) is getType(catalot[targetKey]):
		if attribute == "":
			return True
		else:
			if attribute in catalot[key] and catalot[key][attribute] != catalot[targetKey][attribute]:
				for k in answers: #don't accept the answer if its value is already in answers
					if catalot[key][attribute] == answers[k][attribute]:
						return False
				return True
			else:
				return False
	else:
		return False


#used only to select alternate keys if the strict selection found too few of them; ignores answer type
def isAcceptableAltAnswerKey(catalot, finalAnswers, targetKey, key, attribute):
	if key not in finalAnswers:
		if attribute == "":
			return catalot[key] != catalot[targetKey]
		else:
			return attribute not in catalot[key] or catalot[key][attribute] != catalot[targetKey][attribute]


def getDatePrecision(entry, attribute):
	if attribute == "":
		if getType(entry) is Type.Date:
			return entry.precision()
		else:
			return entry[0].precision()
	else:
		if getType(entry[attribute]) is Type.Date:
			return entry[attribute].precision()
		else:
			return entry[attribute][0].precision()


def getDateTotalDays(entry, attribute):
	if attribute == "":
		if getType(entry) is Type.Date:
			return entry.totalDays()
		else:
			return (entry[0].totalDays() + entry[1].totalDays()) / 2 #take the midpoint date of the range
	else:
		if getType(entry[attribute]) is Type.Date:
			return entry[attribute].totalDays()
		else:
			return (entry[attribute][0].totalDays() + entry[attribute][1].totalDays()) / 2


def getDateDayDifference(entry, attribute, targetTotalDays):
	if attribute == "":
		if getType(entry) is Type.Date:
			return entry.dayDifference(targetTotalDays)
		else:
			return (entry[0].dayDifference(targetTotalDays) + entry[1].dayDifference(targetTotalDays)) / 2
	else:
		if getType(entry[attribute]) is Type.Date:
			return entry[attribute].dayDifference(targetTotalDays)
		else:
			return (entry[attribute][0].dayDifference(targetTotalDays) + entry[attribute][1].dayDifference(targetTotalDays)) / 2


def getAltAnswers(catalot, targetKey, returnKeys, attribute=""):
	answers = {}
	for key in catalot:
		if isAcceptableAltAnswer(catalot, answers, targetKey, key, attribute):
			answers[key] = catalot[key]

	if getType(catalot[targetKey], attribute) is Type.Date or getType(catalot[targetKey], attribute) is Type.DateRange:
		#find difference in days between dates/ranges; discard duplicate answers
		diff = {}
		toDel = []
		targetTotalDays = getDateTotalDays(catalot[targetKey], attribute)
		
		for key in answers:
			if getType(answers[key], attribute) is Type.Date or getType(answers[key], attribute) is Type.DateRange:
				days = getDateDayDifference(answers[key], attribute, targetTotalDays)
			else:
				days = 0

			if days == 0:
				toDel.append(key)
			else:
				diff[key] = days
		
		for key in toDel:
			del answers[key]
		
		#discard dates with different precision (e.g. only year instead of full date)
		toDel.clear()
		targetPrecision = getDatePrecision(catalot[targetKey], attribute)
		
		for key in answers:
			if getDatePrecision(answers[key], attribute) != targetPrecision:
				toDel.append(key)

		for key in toDel:
			if len(answers) <= 5:
				break #keep at least 5 answers whatever their precision
			del answers[key]

		#discard dates that are too close to the target date
		toDel.clear()
		if attribute == "":
			for key in answers:
				if getType(catalot[targetKey]) == getType(answers[key]):
					if getType(catalot[targetKey]) is Type.Date:
						if catalot[targetKey].isAlmostCorrect(answers[key]):
							toDel.append(key)
					else:
						if catalot[targetKey][0].isAlmostCorrect(answers[key][0]) and catalot[targetKey][1].isAlmostCorrect(answers[key][1]):
							toDel.append(key)
		else:
			for key in answers:
				if getType(catalot[targetKey][attribute]) == getType(answers[key][attribute]):
					if getType(catalot[targetKey][attribute]) is Type.Date:
						if catalot[targetKey][attribute].isAlmostCorrect(answers[key][attribute]):
							toDel.append(key)
					else:
						if catalot[targetKey][attribute][0].isAlmostCorrect(answers[key][attribute][0]) and catalot[targetKey][attribute][1].isAlmostCorrect(answers[key][attribute][1]):
							toDel.append(key)

		for key in toDel:
			if len(answers) <= 5:
				break #keep at least 5 answers even if they are too close
			del answers[key]

		#select the 5 closest unique dates (put them at the start of the array)
		minDiffs = [sys.maxsize] * 5
		finalAnswers = [""] * 5

		for key in answers:
			i = 0
			while i < 5 and diff[key] >= minDiffs[i]:
				i += 1

			if i < 5:
				for j in range(4, i, -1):
					minDiffs[j] = minDiffs[j-1]
					finalAnswers[j] = finalAnswers[j-1]

				minDiffs[i] = diff[key]
				finalAnswers[i] = key

		if not returnKeys:
			for i in range(len(finalAnswers)):
				if finalAnswers[i] == "":
					#generate a random date if necessary (if there aren't enough actual dates in the knowledge file)
					if random.randint(0, 1) == 0:
						finalAnswers[i] = Date(str(random.randint(1, datetime.now().year)))
					else:
						finalAnswers[i] = Date('-' + str(random.randint(1, datetime.now().year)))
				else:
					if attribute == "":
						finalAnswers[i] = answers[finalAnswers[i]]
					else:
						finalAnswers[i] = answers[finalAnswers[i]][attribute]
		else:
			if len(answers) < 5:
				#rebuild answers
				for key in catalot:
					if key != targetKey:
						answers[key] = catalot[key]

			for i in range(len(finalAnswers)):
				if len(answers) == 0:
					break

				if finalAnswers[i] == "":
					#grab random keys if necessary (if there aren't enough actual dates in the knowledge file)<
					nextA = random.choice(list(answers.keys()))

					if returnKeys:
						finalAnswers[i] = nextA
					del answers[nextA]
	elif getType(catalot[targetKey], attribute) is Type.Number or getType(catalot[targetKey], attribute) is Type.NumberRange:
		if attribute == "":
			if getType(catalot[targetKey]) is Type.Number:
				targetNumber = catalot[targetKey]
			else:
				targetNumber = (catalot[targetKey][0] + catalot[targetKey][1]) / 2
		else:
			if getType(catalot[targetKey][attribute]) is Type.Number:
				targetNumber = catalot[targetKey][attribute]
			else:
				targetNumber = (catalot[targetKey][attribute][0] + catalot[targetKey][attribute][1]) / 2

		removeSimilarNumbers(targetNumber, answers, attribute)

		#select the 5 closest numbers
		finalAnswers = []

		while len(finalAnswers) < 5 and len(answers) > 0:
			minDiff = sys.maxsize

			for key in answers:
				if attribute == "":
					if getType(answers[key]) is Type.Number:
						diff = abs(answers[key] - targetNumber)
					elif getType(answers[key]) is Type.NumberRange:
						diff = abs((answers[key][0]+answers[key][1])/2 - targetNumber)
					else:
						diff = sys.maxsize
				else:
					if getType(answers[key][attribute]) is Type.Number:
						diff = abs(answers[key][attribute] - targetNumber)
					elif getType(answers[key]) is Type.NumberRange:
						diff = abs((answers[key][attribute][0]+answers[key][attribute][1])/2 - targetNumber)
					else:
						diff = sys.maxsize

				if diff <= minDiff:
					minKey = key
					diff = minDiff

			if attribute == "":
				num = answers[minKey]
			else:
				num = answers[minKey][attribute]

			if returnKeys:
				finalAnswers.append(minKey)
			else:
				finalAnswers.append(num)

			del answers[minKey]
			removeSimilarNumbers(num, answers, attribute)
	elif getType(catalot[targetKey], attribute) is Type.Geo:
		#discard entries of a different geo type
		toDel = []
		targetGeoType, targetGeoName = splitGeoName(catalot[targetKey], attribute)
		
		for key in answers:
			geoType, geoName = splitGeoName(catalot[key], attribute)
		
			if geoType != targetGeoType:
				toDel.append(key)

		for key in toDel:
			if len(answers) <= 5:
				break #keep at least 5 answers whatever their geo type
			del answers[key]

		#select random keys (geo questions only ever require keys (returnKeys is always True))
		finalAnswers = []

		while len(finalAnswers) < 5 and len(answers) > 0:
			nextA = random.choice(list(answers.keys()))
			finalAnswers.append(nextA)
			del answers[nextA]
	else:
		finalAnswers = []

		while len(finalAnswers) < 5 and len(answers) > 0:
			nextA = random.choice(list(answers.keys()))

			if returnKeys:
				finalAnswers.append(nextA)
			else:
				if attribute == "":
					finalAnswers.append(answers[nextA])
				else:
					finalAnswers.append(answers[nextA][attribute])
			
			del answers[nextA]

	if len(finalAnswers) < 5:
		if returnKeys:
			#grab some random keys whatever their type
			for key in catalot:
				if isAcceptableAltAnswerKey(catalot, finalAnswers, targetKey, key, attribute):
					finalAnswers.append(key)
					if len(finalAnswers) == 5:
						break
			random.shuffle(finalAnswers)
		elif getType(catalot[targetKey], attribute) is Type.Number or getType(catalot[targetKey], attribute) is Type.NumberRange:
			#generate some random numbers
			finalAnswers.append(targetNumber) #temporarily add the correct answer

			if getType(catalot[targetKey], attribute) is Type.Number:
				sum = 0
				for a in finalAnswers:
					sum += toInt(a)

				baseJump = sum // len(finalAnswers) // 10
			else:
				baseJump = 0
				for answer in finalAnswers:
					baseJump += (answer[0] + answer[1]) / 2

			if baseJump <= 0:
				baseJump = 1

			while len(finalAnswers) < 6:
				num = toInt(random.choice(finalAnswers))

				if random.randint(0, 1) == 0:
					jump = baseJump
				else:
					jump = -baseJump

				while num in finalAnswers:
					num += jump

				finalAnswers.append(num)

			finalAnswers.remove(targetNumber)

	return finalAnswers


#discard numbers that are too close to the target number
def removeSimilarNumbers(num, answers, attribute):
	toDel = []
	
	for key in answers:
		if attribute == "":
			if getType(answers[key]) is Type.Number:
				a = answers[key]
			elif getType(answers[key]) is Type.NumberRange:
				a = (answers[key][0] + answers[key][1]) / 2
			else:
				continue
		else:
			if getType(answers[key][attribute]) is Type.Number:
				a = answers[key][attribute]
			elif getType(answers[key][attribute]) is Type.NumberRange:
				a = (answers[key][attribute][0] + answers[key][attribute][1]) / 2
			else:
				continue

		if abs(toInt(a) - toInt(num)) / toInt(num) < 0.10:
			toDel.append(key)

	for key in toDel:
		if len(answers) <= 5:
			break #keep at least 5 answers even if they are too close
		del answers[key]


def toInt(x):
	if type(x) is tuple:
		return sum(x) // len(x)
	elif type(x) is str:
		#remove any parentheses and try converting to int
		x = removeParentheses(x)
		try:
			return int(x)
		except:
			return -1
	else:
		return x


def removeParentheses(s, concealContents=False):
	while True:
		try:
			lb = s.index('(')
			ub = lb + 1
			parenthesesDepth = 1
			while ub < len(s) and parenthesesDepth > 0:
				if s[ub] == '(':
					parenthesesDepth += 1
				elif s[ub] == ')':
					parenthesesDepth -= 1
				ub += 1

			if concealContents:
				s = s[:lb] + "[[[???]]]" + s[ub:]
			else:
				s = s[:lb] + s[ub:]
				s = s.rstrip().replace("  ", " ")
		except:
			return s.replace("[[[", "(").replace("]]]", ")")


def fullPath(relativePath):
	if pathHasAudioExtension(relativePath):
		subDir = "!SOUNDS"
	else:
		subDir = "!IMAGES"

	path = DIR + os.sep + subDir + os.sep + relativePath
	if os.path.isabs(path):
		return path
	else:
		return os.getcwd() + os.sep + path


def feedback(msg, playSound=True):
	correct = not "Wrong" in msg

	if msg != "":
		if "Correct answer: False" in msg:
			msg = msg.replace("Correct answer: False", "") #this shouldn't be printed
		if " GEO You selected" in msg:
			msg = msg.replace(" GEO You selected", " You selected") #"GEO" shouldn't be printed

		if correct:
			color = Fore.GREEN
		else:
			color = Fore.RED
		colorPrint(msg, color)

	if playSound:
		if msg == "Entry learned!":
			PlaySound(SOUND_LEARNED, SND_FILENAME)
		elif correct:
			PlaySound(SOUND_CORRECT, SND_FILENAME)
		else:
			PlaySound(SOUND_WRONG, SND_FILENAME)


def colorPrint(text, color, endline="\n"):
	print(color, end="")
	print(text, end=endline)
	print(Fore.RESET, end="")


def printList(items, step, indentLevel, color, firstItem=1):
	stepOffset = 0 #takes into account how many sublists have been printed before the current item (so that we know its correct index)

	for i in range(firstItem, step):
		heading = i < len(items) and type(items[i]) is list

		if type(items[i-1]) is list:
			printList(items[i-1], len(items[i-1])+1, indentLevel+1, color)
			stepOffset -= 1
		elif type(items[i-1]) is tuple:
			item = "{0}. {1}".format(i + stepOffset, toString(items[i-1]))
			if heading:
				colorPrint('\t'*indentLevel + item, color)
			else:	
				print('\t'*indentLevel + item)
		else:
			if heading:
				colorPrint("{0}{1}. {2}".format('\t'*indentLevel, i + stepOffset, items[i-1]), color)
			else:
				print("{0}{1}. {2}".format('\t'*indentLevel, i + stepOffset, items[i-1]))

	return stepOffset


def constructHint(a):
	aType = getType(a)

	if aType is Type.DateRange:
		key = str(a[0]) + " - " + str(a[1])
		hint = '_' * len(str(a[0])) + " - " + '_' * len(str(a[1]))
	else:
		key = str(a)
		hint = ""

		parenthesesDepth = 0
		showFirstLetters = aType is Type.String
		firstLetter = True

		for i in range(len(key)):
			if key[i].isalnum():
				if (showFirstLetters and firstLetter) or parenthesesDepth > 0:
					if key[i].isdigit() and parenthesesDepth == 0:
						hint += '_'
					else:
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
					parenthesesDepth += 1
				elif key[i] == ')':
					parenthesesDepth -= 1

	return hint


def multipleChoice(answers):
	#print choices
	i = 1
	for answer in answers:
		print(str(i) + ". " + toString(answer))
		i += 1

	#wait for user's answer
	choice = ""
	while (choice == "" or not choice.isdigit() or int(choice) < 1 or int(choice) > len(answers)) and "exit" not in choice:
		choice = input("> ")
	choice, exit, immediately = checkForExit(choice)

	try:
		userA = answers[int(choice)-1]
	except:
		userA = ""
	return userA, exit, immediately


def qType_MultipleChoice(catalot, q, a, answers, color):
	aStr = toString(a)
	colorPrint(toString(q), color)

	answers.insert(random.randint(0, len(answers)), a)

	allDates = True
	for answer in answers:
		if getType(answer) is not Type.Date and getType(answer) is not Type.DateRange:
			allDates = False
			break
	if allDates:
		answers.sort() #dates should be sorted before string conversion
		
	#convert to strings
	for i in range(len(answers)):
		answers[i] = toString(answers[i])
	if not allDates:
		answers.sort()

	userA, exit, immediately = multipleChoice(answers)

	correct = userA == aStr
	if not correct and userA != "":
		correct = aStr

		if catalot != None:
			#find the value (or key) associated with the user's wrong answer
			if userA in catalot:
				userAValue = toString(catalot[userA]).replace("\n   ", ", ")
				correct += "\n({0} -> {1})".format(userA, userAValue)
			elif userA in catalot.values():
				userAKey = list(catalot)[list(catalot.values()).index(userA)]
				correct += "\n({0} -> {1})".format(toString(userA), userAKey)
			else:
				#check classes
				for key in catalot:
					if getType(catalot[key]) is Type.Class and userA in catalot[key].values():
						correct += "\n({0} -> {1})".format(toString(userA), key)
						break

	return correct, exit, immediately


def qType_EnterAnswer(q, a, color, catalot=None, attribute="", items=[], alwaysShowHint=False, indentLevel=0, validAnswers=set(), otherNames=set(), acceptOtherNames=True, geoType=""):
	if type(q) is not str:
		q = str(q)
	
	#if q is in the format of "<int>.", then it represents the index of a list item
	if q[-1] == '.':
		try:
			int(q[:-1])
			promptPrefix = q
		except:
			promptPrefix = ">"
	else:
		promptPrefix = ">"

	if promptPrefix == ">":
		colorPrint('\t'*indentLevel + toString(q), color)

	originalA = a
	aStr = toString(a, False)
	aStrReadable = toString(a)
	showHint = alwaysShowHint or len(aStr.split()) > 5 or len(aStr) > 30 #don't show the hint for simple answers

	if getType(a) is Type.Date:
		aIsDate = True
		prompt = "{0}{1} {2}?\n{0}{1} ".format('\t'*indentLevel, promptPrefix, a.precisionPrompt())
		a = repr(a)
	elif getType(a) is Type.DateRange:
		aIsDate = True
		prompt = "{0}{1} {2} - {3}?\n{0}{1} ".format('\t'*indentLevel, promptPrefix, a[0].precisionPrompt(), a[1].precisionPrompt())
		a1Precision = a[1].precision() #used to convert '--' to a representation of today's date
		a = repr(a[0]) + "-" + repr(a[1])
	else:
		aIsDate = False
		if showHint:
			prompt = "{0}{1} {2}\n{0}{1} ".format('\t'*indentLevel, promptPrefix, constructHint(a))
		else:
			prompt = "{0}{1} ".format('\t'*indentLevel, promptPrefix)

	#wait for user's answer
	tryAgain = firstAttempt = True

	while tryAgain:
		answer, exit, immediately = checkForExit(input(prompt))
		tryAgain = False

		result = isAnswerCorrect(answer, a, aIsDate=aIsDate, showFullAnswer=not showHint, indentLevel=indentLevel, otherNames=otherNames, acceptOtherNames=acceptOtherNames, geoType=geoType)
		if result == "try again":
			print('\t'*indentLevel + "Your answer is another name for this entry. Please try again.")
			tryAgain = True
		elif result == True:
			correct = True
		else:
			if catalot != None and a in catalot:
				#check if the user's answer is in validAnswers
				for validAnswer in validAnswers:
					if isAnswerCorrect(answer, validAnswer, aIsDate=aIsDate, showFullAnswer=not showHint, otherNames=otherNames, acceptOtherNames=acceptOtherNames):
						if color == COLOR_UNLEARNED:
							print('\t'*indentLevel + "Your answer is not wrong, but another entry is the expected answer. Please try again.")
							tryAgain = True
						else:
							print('\t'*indentLevel + "Expected answer:", a)
							correct = True

				if ("correct" not in locals() or not correct) and not tryAgain:
					#check if the user's answer is correct for another entry
					if attribute == "":
						for key in catalot:
							if key != a and toString(catalot[key]) == q and isAnswerCorrect(answer, key, aIsDate=aIsDate, showFullAnswer=not showHint, otherNames=otherNames, acceptOtherNames=acceptOtherNames):
								if color == COLOR_UNLEARNED:
									print('\t'*indentLevel + "Your answer is not wrong, but another entry is the expected answer. Please try again.")
									tryAgain = True
								else:
									print('\t'*indentLevel + "Expected answer:", a)
									correct = True
								break
					else:
						for key in catalot:
							if key != a and attribute in catalot[key]:
								#check if entry contains the items in question
								entryContainsAllTimes = True
								for item in items:
									if item not in catalot[key][attribute]:
										entryContainsAllTimes = False
										break

								#is this the user's answer?
								if entryContainsAllTimes and isAnswerCorrect(answer, key, aIsDate=aIsDate, showFullAnswer=not showHint, otherNames=otherNames, acceptOtherNames=acceptOtherNames):
									if color == COLOR_UNLEARNED:
										print('\t'*indentLevel + "Your answer is not wrong, but another entry is the expected answer. Please try again.")
										tryAgain = True
									else:
										print('\t'*indentLevel + "Expected answer:", a)
										correct = True
									break

			if "correct" in locals() and correct:
				break
			elif not tryAgain:
				if aIsDate and firstAttempt:
					if getType(originalA) is Type.Date:
						#check if the user entered the decade in short form (e.g. '60s' instead of '1960s')
						if len(answer) == 3 and answer[2] == 's':
							if answer[:2] == "00" or answer[:2] == "10":
								answer = "20" + answer[:2] + "s"
							else:
								answer = "19" + answer[:2] + "s"

							if isAnswerCorrect(answer, a, aIsDate=aIsDate, showFullAnswer=not showHint, indentLevel=indentLevel, otherNames=otherNames, acceptOtherNames=acceptOtherNames, geoType=geoType):
								correct = True
								break

						#check if the user's answer is relatively close to the correct Date
						if originalA.isAlmostCorrect(answer):
							print('\t'*indentLevel + "Your answer is almost correct. You have one more attempt.")
							tryAgain = True
							firstAttempt = False
					else:
						answerRange = answer.split(' - ')
						#check if the user entered an ongoing DateRange
						if len(answerRange) == 1:
							if len(answer) > 2 and answer[-2:] == '--':
								if a1Precision == 'y':
									a1 = "{0}".format(datetime.now().year)
								elif a1Precision == 'm':
									a1 = "{0}-{1}".format(datetime.now().year, datetime.now().month)
								else: # a1Precision == 'd'
									a1 = "{0}-{1}-{2}".format(datetime.now().year, datetime.now().month, datetime.now().day)

								answer = answer[:-2] + " - " + a1
								answerRange = answer.split(' - ')
								
								if isAnswerCorrect(answer, a, aIsDate=aIsDate, showFullAnswer=not showHint, indentLevel=indentLevel, otherNames=otherNames, acceptOtherNames=acceptOtherNames, geoType=geoType):
									correct = True
									break

						if len(answerRange) == 2:
							#check if the user's answer is relatively close to the correct DateRange
							if originalA[0].isAlmostCorrect(answerRange[0]) and originalA[1].isAlmostCorrect(answerRange[1]):
								print('\t'*indentLevel + "Your answer is almost correct. You have one more attempt.")
								tryAgain = True
								firstAttempt = False
				elif getType(originalA) is Type.Number:
					answer = convertToFullNumber(answer)

					#check if the user's answer is relatively close to the correct Number
					try:
						answer = int(answer)
						relativeError = abs(answer - a) / a

						if relativeError < 0.05:
							#close enough; accept the answer
							print('\t'*indentLevel + "Exact number: " + aStr)
							correct = True
							break
						elif firstAttempt and relativeError < 0.10:
							print('\t'*indentLevel + "Your answer is almost correct. You have one more attempt.")
							tryAgain = True
							firstAttempt = False
					except:
						pass
				elif getType(originalA) is Type.NumberRange and '-' in answer:
					answerRange = answer.split('-')
					answerRange[0] = convertToFullNumber(answerRange[0])
					answerRange[1] = convertToFullNumber(answerRange[1])

					#check if the user's answer is relatively close to the correct NumberRange
					try:
						answerRange = [int(answerRange[0]), int(answerRange[1])]
						maxRelativeError = max(abs(answerRange[0] - a) / a, abs(answerRange[1] - a) / a)

						if maxRelativeError < 0.05:
							#close enough; accept the answer
							print('\t'*indentLevel + "Exact number: " + aStr)
							correct = True
							break
						elif firstAttempt and maxRelativeError < 0.10:
							print('\t'*indentLevel + "Your answer is almost correct. You have one more attempt.")
							tryAgain = True
							firstAttempt = False
					except:
						pass
				else:
					if areWordsSynonyms(answer, aStr):
						if acceptOtherNames:
							print('\t'*indentLevel + "Actual answer: " + aStr)
							correct = True
							break
						else:
							print('\t'*indentLevel + "Your answer is synonymous with the correct answer. Please try again.")
							tryAgain = True
			
			if tryAgain:
				continue
			else:
				correct = aStrReadable

	return correct, exit, immediately


def convertToFullNumber(answer):
	#check if the answer contains a 'k' or an 'm' (shorthand for thousand and million) and replace it with the actual number it represents
	if answer[-1:].lower() == 'k':
		try:
			answer = str(int(float(answer[:-1]) * 1000))
		except:
			pass
	elif answer[-1:].lower() == 'm':
		try:
			answer = str(int(float(answer[:-1]) * 1000000))
		except:
			pass

	return answer


def isAnswerCorrect(answer, a, aIsDate=False, showFullAnswer=False, indentLevel=0, otherNames=set(), acceptOtherNames=True, geoType=""):
	originalAnswer = answer
	aStr = toString(a, False)

	#ignore segments in parentheses
	answer = removeParentheses(answer)
	correctAnswer = removeParentheses(aStr)

	if aIsDate:
		#ensure the same format
		answer = answer.replace("-0", "-")
		correctAnswer = correctAnswer.replace("-0", "-")
	
	#ignore punctuation
	answer = ''.join(e for e in answer.lower() if e.isalnum())
	correctAnswer = ''.join(e for e in correctAnswer.lower() if e.isalnum())

	#check answer
	if getType(a) is Type.String and not aIsDate:
		originalAnswer = answer
		answer = removeTypos(answer, correctAnswer, originalCorrectAnswer=aStr, indentLevel=indentLevel)

		if answer != originalAnswer:
			showFullAnswer = False #don't show full answer if it has already been shown in the typo message

		#if wrong -> check against original correct answer with parentheses
		if answer != correctAnswer:
			originalAnswer = ''.join(e for e in originalAnswer.lower() if e.isalnum()) #ignore punctuation
			tmpCorrectAnswer = ''.join(e for e in aStr.replace('(', '').replace(')', '').lower() if e.isalnum())
			originalAnswer = removeTypos(originalAnswer, tmpCorrectAnswer, originalCorrectAnswer=aStr, indentLevel=indentLevel)

			if originalAnswer == tmpCorrectAnswer:
				answer = correctAnswer
				showFullAnswer = False

		#if wrong -> ignore "the" and "of"; also ignore geoType errors ("Victoria" == "Lake Victoria")
		if answer != correctAnswer:
			tmpAnswer = answer
			tmpCorrectAnswer = correctAnswer

			ignoredWords = ["the", "of", geoType]
			if geoType == "archipelago":
				ignoredWords.append("islands")
			elif geoType == "mountain range":
				ignoredWords.append("mountains")
				ignoredWords.append("range")
			elif geoType == "reservoir":
				ignoredWords.append("lake")
			elif geoType == "lake":
				ignoredWords.append("reservoir")
			elif geoType == "strait":
				ignoredWords.append("passage")
				ignoredWords.append("entrance")

			for ignoredWord in ignoredWords:
				tmpAnswer = tmpAnswer.replace(ignoredWord, "")
				tmpCorrectAnswer = tmpCorrectAnswer.replace(ignoredWord, "")

			tmpAnswerBeforeRemovingTypos = tmpAnswer
			tmpAnswer = removeTypos(tmpAnswer, tmpCorrectAnswer, originalCorrectAnswer=aStr, indentLevel=indentLevel)
			if tmpAnswer != tmpAnswerBeforeRemovingTypos:
				showFullAnswer = False

			if tmpAnswer == tmpCorrectAnswer:
				answer = correctAnswer
				if showFullAnswer:
					print('\t'*indentLevel + "Exact answer: " + aStr)
					showFullAnswer = False

		#if wrong -> check other names
		if answer != correctAnswer and len(otherNames) > 0:	
			for alt in otherNames:
				if isAnswerCorrect(answer, alt, aIsDate=aIsDate, showFullAnswer=True, indentLevel=indentLevel, otherNames=set(), geoType=geoType):
					if acceptOtherNames:
						answer = correctAnswer
						showFullAnswer = False
						break
					else:
						return "try again"
	
	correct = answer == correctAnswer
	
	if correct and showFullAnswer and removeParentheses(aStr) != aStr:
		print('\t'*indentLevel + "Full answer: " + aStr)

	return correct


def qType_FillString(q, s, difficulty, color):
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

	if len(words) == 0:
		words.append(0) #no words to test -> test the first coreword

	if difficulty == 1:
		nBlanks = max(len(words) // 2, 1)
	elif difficulty == 2:
		nBlanks = len(words)
	else:
		nBlanks = len(words)

	blanks = []
	for i in range(nBlanks):
		nextIndex = random.choice(words)
		words.remove(nextIndex)
		blanks.append(nextIndex)

	#print the string with blanks
	colorPrint(q + ":", color)
	print("> ", end="")

	#print hint
	for i in range(len(parts)):
		if i not in blanks:
			print(parts[i], end="")
		else:
			if difficulty == 1:
				print(parts[i][0] + "_" * (len(parts[i])-1), end="") #reveal the first letter
			else:
				print("_" * len(parts[i]), end="") #show only underscores

		if i not in noSpaceAfterThisPart:
			print(" ", end="")

	print("\n> ", end="")

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
			nextPartIsCorrect = extraAnswerChars[:nChars].lower() == parts[i].lower()
			if not nextPartIsCorrect:
				extraAnswerChars = removeTypos(extraAnswerChars, parts[i], returnTruncatedAnswer=False)
				nextPartIsCorrect = extraAnswerChars[:nChars].lower() == parts[i].lower()

			if nextPartIsCorrect:
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

			if answer[:nChars].lower() != parts[i].lower():
				#check for typos
				answer = removeTypos(answer, parts[i], returnTruncatedAnswer=False)
				if answer[:nChars].lower() != parts[i].lower():
					#check for synonyms
					ub = len(answer)
					if ' ' in answer:
						ub = answer.index(' ')
						nChars = ub
					if areWordsSynonyms(answer[:ub], parts[i]):
						print("Actual word:", parts[i])
					else:
						allCorrect = False
						break

			extraAnswerChars = answer[nChars:].rstrip() #if the user entered more than one blank entry, save the rest of his answer

			if exit:
				allCorrect = i == max(blanks) #if this was the last blank part then the answer is allCorrect
				break

			print(" " * (nPrevChars + len(extraAnswerChars) + 1), end="") #align text
	print() #go to a new line because the output might be aligned to the right

	if not allCorrect:
		allCorrect = s

	return allCorrect, exit, immediately


def qType_RecognizeList(listKey, items, color, catalot=None, attribute="", otherNames=set(), acceptOtherNames=True, geoType=""):
	#pick 3 random items
	randomItems = list(items)
	random.shuffle(randomItems)
	randomItems = randomItems[:min(3, len(randomItems))]
	
	for item in randomItems:
		print(toString(item))

	if attribute != "":
		itemsType = "entry"
	elif getType(items) is Type.List:
		itemsType = "list"
	else:
		itemsType = "set"

	if attribute != "":
		attributeName = '(' + attribute + ") "
	else:
		attributeName = ""

	return qType_EnterAnswer("What {} do these {}items belong to? ".format(itemsType, attributeName), listKey, color, catalot=catalot, attribute=attribute, items=randomItems, otherNames=otherNames, acceptOtherNames=acceptOtherNames, geoType=geoType)


def qType_RecognizeItem(listKey, items, color):
	colorPrint(listKey, color)
	index = random.randint(0, len(items)-1)

	item = removeParentheses(toString(items[index], hideDates=True), True) #hide the contents of the parentheses because it might reveal the answer to the user
	answer, exit, immediately = checkForExit(input("What is the index of this item: " + item + "? "))

	try:
		if int(answer)-1 == index:
			return True, exit, immediately
		else:
			return str(index+1), exit, immediately
	except:
		return str(index+1), exit, immediately


def qType_RecognizeClass(catalot, key, color, otherNames, geoType):
	entry = catalot[key]
	usedGUI = False

	for attribute in entry:
		if getType(entry[attribute]) is Type.Image:
			if geoType == "":
				msgGUI("I {}".format(fullPath(entry[attribute])))
				usedGUI = True
			else:
				msgGUI("M {}".format(fullPath(entry[attribute]))) #if the class contains both a map element and an image (probably a flag), show the image in the lower-right corner
		elif getType(entry[attribute]) is Type.Geo:
			geoType, geoName = splitGeoName(entry, attribute)
			msgGUI("map 1 {}".format(geoName))
			usedGUI = True
		elif getType(entry[attribute]) is Type.Sound:
			msgGUI("audio B {}".format(fullPath(entry[attribute])))
			usedGUI = True
		else:
			print(attribute + ": " + toString(entry[attribute]).replace(key, "???"))

	#get a list of other classes that have the same attributes as this one, and accept them as correct answers
	validAnswers = set()
	if not usedGUI: #don't do this if this class has an associated Image, Geo, or Sound, because those attributes should be unique
		for k in catalot:
			if k != key and getType(catalot[k]) is Type.Class:
				addK = True
				for attribute in entry:
					aType = getType(entry[attribute])
					if aType is not Type.Image and aType is not Type.Geo and aType is not Type.Sound:
						if attribute not in catalot[k] or catalot[k][attribute] != entry[attribute]:
							addK = False
							break
				if addK:
					validAnswers.add(k)

	if geoType != "":
		identifier = geoType
	else:
		identifier = "entry"
	correct, exit, immediately = qType_EnterAnswer("What is the name of this {}? ".format(identifier), key, color, catalot=catalot, validAnswers=validAnswers, otherNames=otherNames, acceptOtherNames=False, geoType=geoType)
	return correct, exit, immediately, usedGUI


def qType_OrderItems(listKey, items, color):
	colorPrint(listKey, color)

	#shuffle items
	shuffledItems = list(items)
	shuffledIndices = list([i+1 for i in range(len(items))])

	for i in range(len(items)):
		index = random.randint(i, len(shuffledItems)-1)

		shuffledItems[i], shuffledItems[index] = shuffledItems[index], shuffledItems[i]
		shuffledIndices[i], shuffledIndices[index] = shuffledIndices[index], shuffledIndices[i]

	correctOrder = ""
	for i in range(len(items)):
		correctOrder += " " + str(shuffledIndices.index(i+1) + 1)
	correctOrder = correctOrder[1:]

	#get answer from user
	for i in range(len(shuffledItems)):
		item = toString(shuffledItems[i], hideDates=True)
		if "), (" in item:
			item = item.replace("), (", ", ").replace('(', '').replace(')', '') #item contains several sub items
		item = removeParentheses(item, True) #hide the contents of the parentheses because it might reveal the answer to the user

		if item == "(???)":
			item = toString(shuffledItems[i], hideDates=True) #don't hide items that are just titles

		print("{}. {}".format(i+1, item))

	answer, exit, immediately = checkForExit(input("Enter the correct order of these items: "))

	#cleanup answer
	answer = answer.replace('.', '').replace(',', ' ')
	while "  " in answer:
		answer = answer.replace("  ", " ")

	if answer == correctOrder:
		return True, exit, immediately
	else:
		return correctOrder, exit, immediately


def qType_Image(imageKey, path, learned=False, otherNames=set()):
	if learned:
		color = COLOR_LEARNED
	else:
		color = COLOR_UNLEARNED

	if not learned or random.randint(0, 1) == 0:
		#choose correct image
		correctAnswer = str(random.randint(1, 6))
		msgGUI("C{0} {1}".format(correctAnswer, path))

		colorPrint("Which image represents {}?".format(imageKey), color)
		answer, quit, immediately = checkForExit(input("> "))

		if answer.strip() == correctAnswer:
			return True, quit, immediately
		else:
			return correctAnswer, quit, immediately
	else:
		#identify image
		correctAnswer = imageKey
		msgGUI("I {}".format(path))

		colorPrint("What is this image associated with?", color)
		answer, quit, immediately = checkForExit(input("> "))

		if isAnswerCorrect(answer, correctAnswer, otherNames=otherNames):
			return True, quit, immediately
		else:
			return correctAnswer, quit, immediately


def qType_Sound(soundKey, path, learned=False, otherNames=set()):
	if learned:
		color = COLOR_LEARNED
	else:
		color = COLOR_UNLEARNED

	if not learned or random.randint(0, 1) == 0:
		#choose correct sound
		correctAnswer = str(random.randint(1, 6))
		msgGUI("audio C {}".format(path))

		colorPrint("What sound represents {}?".format(soundKey), color)
		print("(Click to hear the sound, double click to select it)")
		correct, exit, immediately = getFeedbackFromGUI()
		return correct, exit, immediately
	else:
		#identify sound
		correctAnswer = soundKey
		msgGUI("audio I {}".format(path))

		colorPrint("What is this sound associated with?", color)
		answer, quit, immediately = checkForExit(input("> "))

		if isAnswerCorrect(answer, correctAnswer, otherNames=otherNames):
			return True, quit, immediately
		else:
			return correctAnswer, quit, immediately


def qType_Timeline(key, otherNames=set()):
	msgGUI("timeline " + key.replace("'", "").replace('"', '') + " ?")
	colorPrint("What event (???) is highlighted on the timeline?", COLOR_LEARNED)
	answer, quit, immediately = checkForExit(input("> "))

	if isAnswerCorrect(answer, key, otherNames=otherNames):
		return True, quit, immediately
	else:
		return key, quit, immediately


def qType_FamilyTree(catalot, key, otherNames=set()):
	msgGUI("ftree " + exportFamilyTree(catalot, key, True))
	colorPrint("Who (???) is highlighted in the family tree?", COLOR_LEARNED)
	answer, quit, immediately = checkForExit(input("> "))

	if isAnswerCorrect(answer, key, otherNames=otherNames):
		return True, quit, immediately
	else:
		return key, quit, immediately


def quizNumber(catalot, key, step, color, attribute="", otherNames=set()):
	if step == 1:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(catalot, key + ", " + attribute, catalot[key][attribute], getAltAnswers(catalot, key, False, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot, key, catalot[key], getAltAnswers(catalot, key, False), color)
	elif step == 2:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(catalot, attribute + ", " + toString(catalot[key][attribute]), key, getAltAnswers(catalot, key, True, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot, catalot[key], key, getAltAnswers(catalot, key, True), color)
	elif step == 3:
		if attribute != "":
			correct, exit, immediately = qType_EnterAnswer(key + ", " + attribute, catalot[key][attribute], color, catalot=catalot, attribute=attribute, otherNames=otherNames)
		else:
			correct, exit, immediately = qType_EnterAnswer(key, catalot[key], color, catalot=catalot, otherNames=otherNames)
	elif step == 4:
		if attribute != "":
			correct, exit, immediately = qType_EnterAnswer(attribute + ": " + toString(catalot[key][attribute]), key, color, catalot=catalot, attribute=attribute, otherNames=otherNames)
		else:
			correct, exit, immediately = qType_EnterAnswer(toString(catalot[key]), key, color, catalot=catalot, otherNames=otherNames)

	return correct, exit, immediately


def quizString(catalot, key, step, color, attribute=""):
	if step == 1:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(catalot, key + ", " + attribute, catalot[key][attribute], getAltAnswers(catalot, key, False, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot, key, catalot[key], getAltAnswers(catalot, key, False), color)
	elif step == 2:
		if attribute != "":
			correct, exit, immediately = qType_MultipleChoice(catalot, attribute + ", " + toString(catalot[key][attribute]), key, getAltAnswers(catalot, key, True, attribute), color)
		else:
			correct, exit, immediately = qType_MultipleChoice(catalot, catalot[key], key, getAltAnswers(catalot, key, True), color)
	elif step == 3:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 1, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 1, color)
	elif step == 4:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 2, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 2, color)
	elif step == 5:
		if attribute != "":
			correct, exit, immediately = qType_FillString(key + ", " + attribute, catalot[key][attribute], 3, color)
		else:
			correct, exit, immediately = qType_FillString(key, catalot[key], 3, color)

	return correct, exit, immediately


def quizList(listKey, items, step, indentLevel=0, learned=False):
	if learned:
		color = COLOR_LEARNED
	else:
		color = COLOR_UNLEARNED
	if indentLevel == 0:
		colorPrint(listKey + ":", color)

	if type(step) is list:
		subStep = step[1:]
		if len(subStep) == 1:
			subStep = subStep[0] #convert a 1-element array to int
		step = step[0]
	else:
		subStep = 1

	if step <= len(items):
		stepOffset = printList(items, step, indentLevel, color)
		finalStep = False
	else:
		#this is the final step, which demands the user to enter every list item
		finalStep = True
		step = 1
		stepOffset = 0

	correct = playSound = True
	usedGUI = False
	while type(correct) is bool and step <= len(items):
		if type(items[step-1]) is list:
			if finalStep:
				subStep = len(items[step-1]) + 1

			correct, exit, immediately, usedGUIInSublist = quizList("", items[step-1], subStep, indentLevel=indentLevel+1, learned=learned)
			usedGUI = usedGUI or usedGUIInSublist
			stepOffset -= 1

			if immediately:
				break
			elif type(correct) is list:
				step = [step] + correct
			elif type(correct) is int:
				if correct == len(items[step-1]) + 1:
					correct = True #sublist answered successfully
				else:
					step = [step] + [correct]
		elif type(items[step-1]) is set:
			if not finalStep:
				subSetStep = 1
			else:
				subSetStep = 2
			correct, exit, immediately = quizSet("", items[step-1], subSetStep, [], [], color)
		elif type(items[step-1]) is tuple:
			correct = True
			for item in items[step-1]:
				iType = getType(item)
				if iType is Type.Date or iType is Type.DateRange:
					itemCorrect, exit, immediately = qType_EnterAnswer("{}.".format(step + stepOffset), item, color, alwaysShowHint=not finalStep, indentLevel=indentLevel) #pass Dates without converting them to string
				elif iType is Type.Image:
					itemCorrect, exit, immediately = qType_Image(items[step-1][0], fullPath(item), learned=learned)
					usedGUI = True
				else:
					itemCorrect, exit, immediately = qType_EnterAnswer("{}.".format(step + stepOffset), toString(item), color, alwaysShowHint=not finalStep, indentLevel=indentLevel)

				if type(itemCorrect) is not bool:
					correct = itemCorrect
					break
				if exit:
					break
		else:
			a = toString(items[step-1])
			if removeParentheses(a) == "": #skip items that only consist of parenthesized texts
				colorPrint("{}. {}".format(step + stepOffset, a), color)
				correct = True
				exit = immediately = False
				playSound = False
			else:
				correct, exit, immediately = qType_EnterAnswer("{}.".format(step + stepOffset), a, color, alwaysShowHint=not finalStep, indentLevel=indentLevel)

		if immediately:
			break
		elif learned: #if testing learned entry, only one list item is needed
			printList(items, len(items) + 1, indentLevel, color, step + 1) #print the rest of the list
			return correct, exit, immediately, usedGUI
		elif type(correct) is bool:
			step += 1
			if playSound:
				feedback("")
		elif type(correct) is str and correct != "False":
			feedback("Wrong! Correct answer: " + correct)

	if not finalStep:
		return step, exit, immediately, usedGUI
	elif step == len(items) + 1:
		return True, exit, immediately, usedGUI
	else:
		return "False", exit, immediately, usedGUI #returning "False" instead of False because the script uses the type of that variable to check if correct, not the value


def quizSet(setKey, items, step, color, geoType=""):
	if len(items) == 1:
		#for sets with only 1 item switch to qType_FillString()
		if step == 1:
			fillStringStep = 1
		else:
			fillStringStep = 3
		return qType_FillString(setKey, list(items)[0], fillStringStep, color)

	itemsCopy = list(items)
	hasSubSets, itemsSets = unwrapSets(itemsCopy)

	nSubSets = max(itemsSets)
	if 0 in itemsSets: #0 represents items that don't belong to sub sets
		nSubSets += 1

	itemsLCaseWithoutParentheses = list(itemsCopy)
	itemsLCaseWithoutParentheses = [removeParentheses(item.lower()) for item in itemsLCaseWithoutParentheses]
	correct = True
	setOfPreviousAnswer = -1
	itemsSetJumps = 0

	#print heading and hints
	if nSubSets == 1:
		colorPrint(setKey, color)
	else:
		colorPrint("{0} ({1} in {2}):".format(setKey, pluralizeIfNecessary(len(itemsCopy), "item"), pluralizeIfNecessary(nSubSets, "set")), color)

	showFullAnswer = False
	for item in itemsCopy:
		print("> " + constructHint(item))

	while len(itemsLCaseWithoutParentheses) > 0:
		answer, exit, immediately = checkForExit(input("> "))
		answer = removeParentheses(answer.lower())

		if exit:
			break
		elif answer in itemsLCaseWithoutParentheses:
			pass #continue to process correct answer
		else:
			#check for typos and synonyms
			acceptAnswer = False

			for i in range(len(itemsCopy)):
				if isAnswerCorrect(answer, itemsCopy[i], showFullAnswer=showFullAnswer, geoType=geoType):
					acceptAnswer = True
					break
				elif areWordsSynonyms(answer, itemsCopy[i]):
					print("Actual answer: " + itemsCopy[i])
					acceptAnswer = True
					break

			if acceptAnswer:
				answer = itemsLCaseWithoutParentheses[i]
			else:
				correct = ""
				for item in itemsCopy:
					correct += item + ", "
				correct = correct[:-2]
				break

		#process correct answer
		index = itemsLCaseWithoutParentheses.index(answer)

		if itemsCopy[index].lower() != itemsLCaseWithoutParentheses[index] and showFullAnswer:
			fullAnswer = "Full answer: " + itemsCopy[index] + ". "
		else:
			fullAnswer = ""

		if setOfPreviousAnswer != -1 and setOfPreviousAnswer != itemsSets[index]:
			itemsSetJumps += 1
		setOfPreviousAnswer = itemsSets[index]

		del itemsLCaseWithoutParentheses[index]
		del itemsCopy[index]
		del itemsSets[index]
		
		if fullAnswer != "":
			print(fullAnswer)

	answeredOutOfOrder = itemsSetJumps >= nSubSets #user mixed items from different subSets while answering
	if type(correct) is bool and hasSubSets and answeredOutOfOrder:
		print("Properly grouped items:")
		for item in items:
			print(toString(item))

	return correct, exit, immediately


#removes all sets and adds their values to the list
def unwrapSets(items):
	foundSubSets = False
	sets = getSetsInList(items)
	itemsSets = [0] * len(items) #used to differentiate items from different sub sets
	itemsSetInd = 1

	while len(sets) > 0:
		foundSubSets = True
		for s in sets:
			ind = items.index(s)
			items.remove(s)
			items[ind:ind] = list(s)

			del itemsSets[ind]
			itemsSets[ind:ind] = [itemsSetInd] * len(s)
			itemsSetInd += 1
		sets = getSetsInList(items)

	return foundSubSets, itemsSets


def getSetsInList(items):	
	sets = []
	for item in items:
		if getType(item) is Type.Set:
			sets.append(item)
	return sets


def splitGeoName(geoName, attribute):
	 #ignore "GEO:"
	if attribute == "":
		geoName = geoName[4:]
	else:
		geoName = geoName[attribute][4:]

	separator = geoName.index('/')
	return geoName[:separator].lower(), geoName[separator+1:]


def quizGeo(catalot, key, step, color, attribute="", otherNames=set()):
	if attribute != "" and type(step) is dict:
		step = step[attribute]

	geoType, geoName = splitGeoName(catalot[key], attribute)
	msgGUI("map {} {}".format(step, geoName))

	if step == 1:
		correct, exit, immediately = qType_MultipleChoice(None, "What is the name of the highlighted {}?".format(geoType), key, getAltAnswers(catalot, key, True, attribute), color)
	elif step == 2:
		colorPrint(key, color)
		print("Select this {} on the map.".format(geoType))
		correct, exit, immediately = getFeedbackFromGUI(catalot)
	elif step == 3:
		colorPrint(key, color)
		print("Find the {} on the map.".format(geoType))
		correct, exit, immediately = getFeedbackFromGUI(catalot)
	elif step == 4:
		correct, exit, immediately = qType_EnterAnswer("What is the name of the highlighted {}?".format(geoType), key, color, otherNames=otherNames, geoType=geoType)
		#correct, exit, immediately = qType_FillString("What is the name of the highlighted {}?".format(geoType), key, 5, [], color) #switch to FillString?

	return correct, exit, immediately


def quiz(category, catalot, metacatalot):
	ready, nNew, nLearned = getReadyKeys(metacatalot)
	if nNew + nLearned == 0:
		return
	nCorrect = 0
	nTested = 0

	print("Category: " + category)

	while len(ready) > 0:
		#prepare next question
		print("\n")
		
		key = random.choice(ready)
		entry = catalot[key]
		entryType = getType(entry)
		meta = metacatalot[key]
		step = meta["step"]
		usedGUI = False

		if not meta["learned"]:
			color = COLOR_UNLEARNED

			if entryType is Type.Number or entryType is Type.NumberRange or entryType is Type.Date or entryType is Type.DateRange:
				correct, exit, immediately = quizNumber(catalot, key, step, color)
			elif entryType is Type.Diagram:
				msgGUI("I {}".format(fullPath(entry[0])))
				correct, exit, immediately, usedGUI = quizList(key, entry[1], step)
				usedGUI = True
			elif entryType is Type.Image:
				usedGUI = True
				correct, exit, immediately = qType_Image(key, fullPath(entry), False)
			elif entryType is Type.Sound:
				usedGUI = True
				correct, exit, immediately = qType_Sound(key, fullPath(entry), False)
			elif entryType is Type.String:
				correct, exit, immediately = quizString(catalot, key, step, color)
			elif entryType is Type.Geo:
				correct, exit, immediately = quizGeo(catalot, key, step, color)
				usedGUI = True
			elif entryType is Type.Class:
				#custom class
				if "Other names" in entry:
					otherNames = entry["Other names"]
				else:
					otherNames = set()

				if "Map" in entry:
					geoType, geoName = splitGeoName(entry, "Map")
				else:
					geoType = ""

				#select attributes not yet learned
				correct = {}
				unlearnedAttributes = []

				for attribute in entry:
					if not isLearned(step[attribute], entry[attribute]):
						unlearnedAttributes.append(attribute)

				if "Map" in unlearnedAttributes and unlearnedAttributes[0] != "Map":
					#set the Map attribute as the first element so it gets tested first
					index = unlearnedAttributes.index("Map")
					unlearnedAttributes[0], unlearnedAttributes[index] = unlearnedAttributes[index], unlearnedAttributes[0]

				if unlearnedAttributes == []:
					correct["__finalStep__"], exit, immediately, usedGUI = qType_RecognizeClass(catalot, key, color, otherNames, geoType) #final step
				else:
					keepGUIActive = learnedDateAttribute = False
					usedGUIVisually = False

					for attribute in entry:
						if attribute not in unlearnedAttributes:
							correct[attribute] = "already learned"
							colorPrint("{}: already learned".format(attribute), COLOR_LEARNED)

							if getType(entry[attribute]) is Type.Image and isLearned(step[attribute], entry[attribute]) and not usedGUIVisually:
								#if the class has an image and it has been learned already, show it
								msgGUI("I {}".format(fullPath(entry[attribute])))
								usedGUI = usedGUIVisually = keepGUIActive = True
							elif getType(entry[attribute]) is Type.Geo and isLearned(step[attribute], entry[attribute]) and not usedGUIVisually:
								#if the class has an map element and it has been learned already, show it
								geoType, geoName = splitGeoName(entry, attribute)
								msgGUI("map 1 {}".format(geoName))
								usedGUI = usedGUIVisually = keepGUIActive = True
							elif getType(entry[attribute]) is Type.Sound and isLearned(step[attribute], entry[attribute]):
								#if the class has a sound and it has been learned already, play it
								msgGUI("audio B {}".format(fullPath(entry[attribute])))
								usedGUI = keepGUIActive = True
							elif (getType(entry[attribute]) is Type.Date or getType(entry[attribute]) is Type.DateRange) and isLearned(step[attribute], entry[attribute]):
								#if the class has an date and it has been learned already, show it (only if there are no other attributes that use the GUI visually)
								learnedDateAttribute = True

					if not usedGUI and learnedDateAttribute:
						msgGUI("timeline " + key)
						usedGUI = usedGUIVisually = keepGUIActive = True
					
					#ask a question for each unlearned attribute
					firstQuestion = True

					for attribute in unlearnedAttributes:
						if firstQuestion:
							firstQuestion = False
						else:
							print("\n")
						
						attributeType = getType(entry[attribute])
						if attributeType is Type.Number or attributeType is Type.NumberRange or attributeType is Type.Date or attributeType is Type.DateRange:
							correct[attribute], exit, immediately = quizNumber(catalot, key, step[attribute], color, attribute, otherNames=otherNames)
						elif attributeType is Type.Diagram:
							msgGUI("I {}".format(fullPath(entry[attribute][0])))
							usedGUI = True
							correct[attribute], exit, immediately, usedGUI = quizList(key, entry[attribute][1], step[attribute])
						elif attributeType is Type.Image:
							usedGUI = True
							correct[attribute], exit, immediately = qType_Image(key, fullPath(entry[attribute]), False, otherNames=otherNames)
						elif attributeType is Type.Sound:
							usedGUI = True
							correct[attribute], exit, immediately = qType_Sound(key, fullPath(entry[attribute]), False, otherNames=otherNames)
						elif attributeType is Type.String:
							correct[attribute], exit, immediately = quizString(catalot, key, step[attribute], color, attribute)
						elif attributeType is Type.Geo:
							correct[attribute], exit, immediately = quizGeo(catalot, key, step, color, attribute, otherNames=otherNames)
							usedGUI = True
						elif attributeType is Type.List:
							correct[attribute], exit, immediately, usedGUI = quizList(key + ", " + attribute, entry[attribute], step[attribute])
						elif attributeType is Type.Set:
							correct[attribute], exit, immediately = quizSet(key + ", " + attribute, entry[attribute], step[attribute], color, geoType=geoType)

						if not immediately:
							if type(correct[attribute]) is int or type(correct[attribute]) is list:
								print("List progress @ {}%.".format(100*(correct[attribute]-1)//len(entry[attribute])))
							elif type(correct[attribute]) is not str:
								feedback("Correct!")
							elif "FalseGEO" in correct[attribute]:
								feedback("Wrong!" + correct[attribute].replace("FalseGEO", ""))
							elif correct[attribute] != "False": #if it is "False" then quizList has already printed the correct answer
								feedback(("Wrong! Correct answer: {}").format(correct[attribute]))

							if usedGUI and not keepGUIActive:
								msgGUI("logo")
								usedGUI = False

						if exit:
							break
			elif entryType is Type.List:
				correct, exit, immediately, usedGUI = quizList(key, entry, step)
			elif entryType is Type.Set:
				correct, exit, immediately = quizSet(key, entry, step, color)
		else:
			color = COLOR_LEARNED

			if entryType is Type.Number or entryType is Type.NumberRange or entryType is Type.DateRange:
				correct, exit, immediately = quizNumber(catalot, key, random.randint(1, 4), color)
			elif entryType is Type.Date:
				if random.randint(0, 1) == 0:
					correct, exit, immediately = quizNumber(catalot, key, random.randint(1, 4), color)
				else:
					correct, exit, immediately = qType_Timeline(key)
					usedGUI = True
			elif entryType is Type.Diagram:
				msgGUI("I {}".format(fullPath(entry[0])))
				usedGUI = True
				correct, exit, immediately = qType_RecognizeItem(key, entry[1], color)
			elif entryType is Type.Image:
				usedGUI = True
				correct, exit, immediately = qType_Image(key, fullPath(entry), True)
			elif entryType is Type.Sound:
				usedGUI = True
				correct, exit, immediately = qType_Sound(key, fullPath(entry), True)
			elif entryType is Type.Geo:
				correct, exit, immediately = quizGeo(catalot, key, random.randint(1, 4), color)
				usedGUI = True
			elif entryType is Type.String:
				correct, exit, immediately = quizString(catalot, key, random.randint(1, 4), color) #don't test learned entries on hardest difficulty
			elif entryType is Type.Class:
				otherNames = set()
				for otherNamesAttribute in ["Other names", "Name"]:
					if otherNamesAttribute in entry:
						otherNames = entry[otherNamesAttribute]
						break
				if getType(otherNames) is not Type.Set:
					otherNames = {otherNames}

				if "Map" in entry:
					geoType, geoName = splitGeoName(entry, "Map")
				else:
					geoType = ""

				nFTreeAttributes = 0
				if "Parents" in entry:
					nFTreeAttributes += 1
				if "Consort" in entry:
					nFTreeAttributes += 1

				if random.randint(0, len(entry)) == 0:
					correct, exit, immediately, usedGUI = qType_RecognizeClass(catalot, key, color, otherNames, geoType)
				elif random.randint(0, len(entry)-1) < nFTreeAttributes and random.randint(0, 1) == 0:
					correct, exit, immediately = qType_FamilyTree(catalot, key, otherNames=otherNames)
					usedGUI = True
				else:
					attribute = random.choice(list(entry.keys()))
					attributeType = getType(entry[attribute])

					showTimeline = False
					if attributeType is not Type.Image and attributeType is not Type.Diagram:
						hasDate = False

						#show class image (if any)
						for attr in entry:
							if getType(entry[attr]) is Type.Image:
								msgGUI("I {}".format(fullPath(entry[attr])))
								usedGUI = True
								break
							elif getType(entry[attr]) is Type.Date or getType(entry[attr]) is Type.DateRange:
								hasDate = True

						if not usedGUI and hasDate: #show date on the timeline
							showTimeline = True #msgGUI is called later after the random question type has been determined (because the timeline mustn't show the entry key to the user if that is the answer to the question)

					if attributeType is not Type.Geo and not usedGUI:
						#show map element (if any)
						for attr in entry:
							if getType(entry[attr]) is Type.Geo:
								geoType, geoName = splitGeoName(entry, attr)
								msgGUI("map 1 {}".format(geoName))

					if attributeType is not Type.Sound:
						#play class sound (if any)
						for attr in entry:
							if getType(entry[attr]) is Type.Sound:
								msgGUI("audio B {}".format(fullPath(entry[attr])))
								usedGUI = True
								break

					if attributeType is Type.Number or entryType is Type.NumberRange:
						qType = random.randint(1, 4)

						if showTimeline:
							if qType == 2 or qType == 4:
								msgGUI("timeline " + key + " ?")
							else:
								msgGUI("timeline " + key)
							usedGUI = True

						correct, exit, immediately = quizNumber(catalot, key, qType, color, attribute, otherNames=otherNames)
					elif attributeType is Type.Date or attributeType is Type.DateRange:
						if random.randint(0, 1) == 0:
							correct, exit, immediately = quizNumber(catalot, key, random.randint(1, 4), color, attribute, otherNames=otherNames)
						else:
							correct, exit, immediately = qType_Timeline(key, otherNames=otherNames)
							usedGUI = True
					elif attributeType is Type.Diagram:
						msgGUI("I {}".format(fullPath(entry[attribute][0])))
						usedGUI = True
						if random.randint(0, 1) == 0:
							correct, exit, immediately, usedGUI = quizList(key, entry[attribute][1], random.randint(1, len(entry[attribute][1])), learned=True)
						else:
							correct, exit, immediately = qType_RecognizeItem(key, entry[attribute][1], color)
					elif attributeType is Type.Image:
						usedGUI = True
						correct, exit, immediately = qType_Image(key, fullPath(entry[attribute]), True, otherNames=otherNames)
					elif attributeType is Type.Sound:
						usedGUI = True
						correct, exit, immediately = qType_Sound(key, fullPath(entry[attribute]), True, otherNames=otherNames)
					elif attributeType is Type.String:
						qType = random.randint(1, 5)

						if showTimeline:
							if qType == 2:
								msgGUI("timeline " + key + " ?")
							else:
								msgGUI("timeline " + key)
							usedGUI = True

						correct, exit, immediately = quizString(catalot, key, qType, color, attribute)
					elif attributeType is Type.Geo:
						correct, exit, immediately = quizGeo(catalot, key, random.randint(1, 4), color, attribute, otherNames=otherNames)
						usedGUI = True
					elif attributeType is Type.List:
						qType = random.choice([quizList, qType_RecognizeList, qType_RecognizeItem, qType_OrderItems])

						if showTimeline:
							if qType == qType_RecognizeList:
								msgGUI("timeline " + key + " ?")
							else:
								msgGUI("timeline " + key)
							usedGUI = True

						if qType is quizList:
							correct, exit, immediately, usedGUI = qType(key + ", " + attribute, entry[attribute], random.randint(1, len(entry[attribute])), learned=True)
						elif qType is qType_RecognizeList:
							correct, exit, immediately = qType(key + ", " + attribute, entry[attribute], color, otherNames=otherNames, geoType=geoType)
						else:
							correct, exit, immediately = qType(key + ", " + attribute, entry[attribute], color)
					elif attributeType is Type.Set:
						if random.randint(0, 1) == 0:
							correct, exit, immediately = quizSet(key + ", " + attribute, entry[attribute], 1, color, geoType=geoType)
						else:
							acceptOtherNames = attribute != "Other names" #don't accept other names as answers if they are displayed to the user
							correct, exit, immediately = qType_RecognizeList(key, entry[attribute], color, catalot=catalot, attribute=attribute, otherNames=otherNames, acceptOtherNames=acceptOtherNames, geoType=geoType)
			elif entryType is Type.List:
				qType = random.choice([quizList, qType_RecognizeList, qType_RecognizeItem, qType_OrderItems])

				if qType == quizList:	
					correct, exit, immediately, usedGUI = qType(key, entry, random.randint(1, len(entry)), learned=True)
				else:
					correct, exit, immediately = qType(key, entry, color)
			elif entryType is Type.Set:
				if random.randint(0, 1) == 0:
					correct, exit, immediately = quizSet(key, entry, 1, color)
				else:
					correct, exit, immediately = qType_RecognizeList(key, entry, color, catalot=catalot)

		if usedGUI:
			msgGUI("logo")

		if not immediately:
			#log result
			#the variable correct will be bool if the user entered the right answer. If he failed, correct will be a string holding the actual correct answer
			#for classes correct will be a dictionary containing this representation (bool or string) for every attribute
			if type(correct) is dict:
				#class entry
				if not meta["learned"]:
					if "__finalStep__" not in correct:
						#advance any attributes that have been correctly answered
						anyMistakes = False
						entryProgress = 0
						entryProgressMax = 0

						for attribute in correct:
							if correct[attribute] == "already learned":
								pass
							elif type(correct[attribute]) is bool:
								meta["step"][attribute] += 1
								#skip some useless steps for certain types of attributes (the steps that ask the user to type in the name of the entry)
								attributeType = getType(entry[attribute])
								if attributeType is Type.String and meta["step"][attribute] == 2:
									meta["step"][attribute] += 1
								if (attributeType is Type.Number or attributeType is Type.NumberRange or attributeType is Type.Date or attributeType is Type.DateRange) and (meta["step"][attribute] == 2 or meta["step"][attribute] == 4):
									meta["step"][attribute] += 1
							elif type(correct[attribute]) is int or type(correct[attribute]) is list:
								meta["step"][attribute] = correct[attribute]
							else:
								anyMistakes = True

							entryProgress += meta["step"][attribute]
							entryProgressMax += maxSteps(entry[attribute]) + 1

						if not anyMistakes:
							nCorrect += 1

						print("Entry progress @ {}%.".format(100 * entryProgress // entryProgressMax))
						meta["nextTest"] = datetime.now() + timedelta(hours=22)
					else:
						if correct["__finalStep__"] == True:
							feedback("Correct!")
							feedback("Entry learned!")
							meta["learned"] = True
							meta["nextTest"] = datetime.now() + timedelta(days=6, hours=22)
							nCorrect += 1
						else:
							feedback("Wrong! Correct answer: " + key)
							print("Entry progress @ 100%.")
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
			elif type(correct) is int or type(correct) is list:
				#quizList returns correct as the new step
				newStep = correct
				if type(correct) is list:
					newStepRoot = newStep[0]
				else:
					newStepRoot = newStep
				if type(meta["step"]) is list:
					metaStepRoot = meta["step"][0]
				else:
					metaStepRoot = meta["step"]

				if newStepRoot > metaStepRoot:
					correct = True
					nCorrect += 1
				else:
					correct = False

				if entryType is Type.List:
					print("List progress @ {}%.".format(100*(newStepRoot-1)//len(entry)))
				else:
					print("Diagram progress @ {}%.".format(100*(newStepRoot-1)//len(entry[1])))

				meta["step"] = newStep
				meta["nextTest"] = datetime.now() + timedelta(hours=22)
			else:
				#standard entry type (int, tuple, str) or quizList that returned True or "False" or learned class that only tests 1 attribute
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
								print("Entry progress @ {}%.".format(100*(meta["step"]-1)//maxSteps(entry)))
								meta["nextTest"] = datetime.now() + timedelta(hours=22)
					else:
						if "FalseGEO" in correct:
							feedback("Wrong!" + correct.replace("FalseGEO", ""))
						elif correct != "False": #if it is "False" then quizList has already printed the correct answer
							feedback("Wrong! Correct answer: " + correct)
						
						if entryType is Type.List or entryType is Type.Diagram:
							print("Entry progress @ 100%.")
						else:
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
						meta["nextTest"] = datetime.now() + timedelta(hours=22)

						if entryType is Type.Class:
							for attribute in meta["step"]:
								meta["step"][attribute] = 1
						else:
							meta["step"] = 1

			meta["lastTest"] = datetime.now()
			nTested += 1

		saveToFile(catalot, metacatalot, DIR + os.sep + category + ".txt") #save changes

		ready.remove(key)
		if exit:
			break

	if len(ready) == 0:
		print("\nNo more entries. ", end="")
	if nTested > 0:
		print("Score: {0} / {1} ({2}%)".format(nCorrect, nTested, 100*nCorrect//nTested))
	print("\n")


def exploreMap(key, alot):
	#if no step was specified, set step to 1
	if len(key) > 2 and key[0].isdigit() and key[1] == ' ':
		step = key[0]
		key = key[2:]
	else:
		step = '1'

	#find the actual geographical name for the key
	geoName = key #if the search fails, try with the given key
	for category in alot:
		if key in alot[category]:
			if getType(alot[category][key]) is Type.Geo:
				geoName = alot[category][key]
				break
			elif getType(alot[category][key]) is Type.Class:
				for attribute in alot[category][key]:
					if getType(alot[category][key][attribute]) is Type.Geo:
						geoName = alot[category][key][attribute]
						break

	if '/' in geoName:
		geoName = geoName[geoName.rfind('/')+1:] #strip "GEO:type/" from the name

	msgGUI("map explore {0} {1}".format(step, geoName))
	input("Press Enter to exit map exploration mode...")
	msgGUI("logo")


def mainLoop(alot, metalot):
	choice = ""
	while choice != "exit":
		maxLen = getMaxKeyLen(alot)
		
		print(("\n\t{0:<" + maxLen + "}Ready entries (new / learned)").format("File"))
		totalNew = 0
		totalLearned = 0
		i = 0
		cats = {}

		for category in alot:
			keys, nNew, nLearned = getReadyKeys(metalot[category])
			if nNew + nLearned > 0:
				totalNew += nNew
				totalLearned += nLearned
				i += 1
				cats[i] = category
				print(("{0:<8}{1:<" + maxLen + "}").format(str(i)+'.', category), end="")
				if nNew > 0:
					colorPrint(nNew, COLOR_UNLEARNED, endline="")
				else:
					print(0, end="")
				print(" / ", end="")
				if nLearned > 0:
					colorPrint(nLearned, COLOR_LEARNED)
				else:
					print(0)
			else:
				print(("\t{0:<" + maxLen + "}Available in {1}").format(category, timeUntilAvailable(metalot[category])))
	
		wordChoices = ["help", "exit", "timeline"]
		if totalNew + totalLearned > 0:
			i += 1
			cats[i] = "all"
			wordChoices.append("all")
		
			print(("{0:<8}{1:<" + maxLen + "}").format(str(i)+'.', "all"), end="")
			if totalNew > 0:
				colorPrint(totalNew, COLOR_UNLEARNED, endline="")
			else:
				print(0, end="")
			print(" / ", end="")
			if totalLearned > 0:
				colorPrint(totalLearned, COLOR_LEARNED)
			else:
				print(0)

		cats[i+1] = "help"
		print(("{0:<8}{1:<" + maxLen + "}").format(str(i+1)+'.', "help"))

		cats[i+2] = "exit"
		print(("{0:<8}{1:<" + maxLen + "}\n").format(str(i+2)+'.', "exit"))

		choice = ""
		while choice not in alot and not (choice.isdigit() and int(choice) in cats.keys()) and ("map " not in choice or len(choice) <= 4) and ("unlearn " not in choice or len(choice) <= 8) and choice not in wordChoices:
			choice = input('Choose a category: ')
		if choice.isdigit():
			choice = cats[int(choice)]

		if choice == "help":
			print("List of other commands:\n\tmap [key]\tShows the specified object on the map\n\ttimeline\tNOT YET IMPLEMENTED\n\tunlearn [key]\tResets the progress of an entry")
			input("Press Enter to return to the menu...")
		elif choice == "timeline":
			pass
			#exportTimelineForGUI(alot)
			#msgGUI("timeline")
		elif choice[:4] == "map ":
			choice = choice[4:]
			exploreMap(choice, alot)
		elif choice[:8] == "unlearn ":
			choice = choice[8:]
			foundIn = []

			for category in alot:
				if choice in alot[category]:
					foundIn.append(category)

			if len(foundIn) == 0:
				print("Entry not found!")
				category = ""
			elif len(foundIn) == 1:
				category = foundIn[0]
			else:
				print("Multiple entries found! Choose which one to unlearn:")
				category, exit, immediately = multipleChoice(foundIn)
				if exit:
					category = ""

			if category != "":
				meta = metalot[category][choice]
				if not meta["learned"]:
					print("Entry is not learned!")
				else:
					meta["learned"] = False
					meta["nextTest"] = datetime.now() + timedelta(hours=22)

					if type(meta["step"]) is dict:
						for attribute in meta["step"]:
							meta["step"][attribute] = 1
					else:
						meta["step"] = 1

					saveToFile(alot[category], metalot[category], DIR + os.sep + category + ".txt") #save changes
					print("Entry unlearned!")
		elif choice == "all": #test all categories one by one
			for category in alot:
				quiz(category, alot[category], metalot[category])
				keys, nNew, nLearned = getReadyKeys(metalot[category])
				if nNew + nLearned > 0:
					break
		elif choice != "exit":
			quiz(choice, alot[choice], metalot[choice])

	return False


#MAIN
print("Alot of Knowledge")

init() #colorama init

#load corewords
if os.path.isfile("corewords.txt"):
	with open("corewords.txt") as f:
		corewords = f.read().split()
else:
	corewords = ["BibleThump"]

#load thesaurus
if os.path.isfile("mobythes.aur"):
	with open("mobythes.aur") as f:
		thesaurus = f.readlines()
else:
	thesaurus = ["FeelsBadMan"]

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

try:
	for filename in os.listdir(DIR):
		if ".txt" in filename:
			category = os.path.splitext(filename)[0]
			alot[category], metalot[category], nNew, nExp, nRed, nDel = parseFile(DIR + os.sep + filename)

			if nNew > 0 or nExp > 0 or nRed > 0 or nDel > 0:
				print(filename + " changed: ", end="")
				if nNew > 0:
					print(pluralizeIfNecessary(nNew, "new entry"), end="")
					
					if nExp + nRed + nDel > 0:
						print(", ", end="")
					else:
						print("")
				if nExp > 0:
					print(pluralizeIfNecessary(nExp, "expanded entry"), end="")
					if nDel + nRed > 0:
						print(", ", end="")
					else:
						print("")
				if nRed > 0:
					print(pluralizeIfNecessary(nRed, "reduced entry"), end="")
					if nDel > 0:
						print(", ", end="")
					else:
						print("")
				if nDel > 0:
					print(pluralizeIfNecessary(nDel, "deletion"))

				saveToFile(alot[category], metalot[category], DIR + os.sep + category + ".txt") #save changes
except:
	exceptionMsg = str(traceback.format_exception(*sys.exc_info()))
	if "'SystemExit: 0\\n'" not in exceptionMsg: #if this line is in the msg it means alot detected two entries with the same key and has already printed an error message
		print("\nError while loading knowledge files:\n" + exceptionMsg)
	input("\nPress Enter to exit...")
	sys.exit(0)

#init GUI
exportTimelineForGUI(alot)

gui = subprocess.Popen(GUI)
sleepInterval = 0.125

print("Establishing connection to AlotGUI...", end="")

while "pipe" not in locals():
	try:
		pipe = open(r'\\.\pipe\alotPipe', 'r+b', 0)
	except:
		#give gui time to start the pipe
		print("\rFailed to establish connection to AlotGUI. Next attempt in {}s...".format(sleepInterval), end="")
		sleep(sleepInterval)
		sleepInterval *= 2

print("\r\t\t\t\t\t\t\t\t\t", end="") #erase previous line
print("\rEstablished connection to AlotGUI.")

#show "main menu"
try:
	crashed = mainLoop(alot, metalot)
except:
    print("\nUh-oh: " + str(traceback.format_exception(*sys.exc_info())))
    crashed = True

if crashed:
	input("\nPress Enter to exit...")

gui.terminate()