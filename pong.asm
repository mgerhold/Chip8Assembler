# register usage:
# ~~~~~~~~~~~~~~~
#
# V0 - temporary values
# V1 - constant 1
# V2 - delay timer
# V3 - player 1 score
# V4 - keycodes for comparison
# V5 - player 2 score
# V6 - ball x-velocity (1 = right, 0 = left)
# V7 - ball y-velocity (1 = up, 0 = down)
# V8 - ball x
# V9 - ball y
# VA - player 1 x
# VB - player 1 y
# VC - player 2 x
# VD - player 2 y
# VE -
# VF - collision detection

init:
	# store player sprite
	LOAD	V0, $80
	LOAD	V1, $80
	LOAD	V2, $80
	LOAD	V3, $80
	LOAD	V4, $80
	LOADI	$100
	STOR	V4
	# set player positions
	CALL	resetplayers

	# set player score
	CALL	resetscore

	# constant 1
	LOAD	V1, $1

	# start timer
	CALL	resettimer

	# reset ball
	CALL	resetball

mainloop:
	# handle input and update
	MOVED	V2			# get current delay timer value
	SKE		V2, $0		# skip update subroutine if timer is not zero
	JUMP	render
	CALL	handleinput
	CALL	updateball
	CALL	checkwinner

	render:
	CLR
	CALL	drawplayers
	CALL	drawball
	CALL	drawscore
	JUMP	mainloop

handleinput:
	p1up:				# p1up
	LOAD	V4, $C		# C key = player 1 up
	SKPR	V4
	JUMP	p1down		# this is skipped if up is pressed	
	SKE		VB, $0		# don't move if y value is 0
	SUB		VB, V1		# VB = VB - V1 (V1 = 1)
	JUMP	p1down

	p1down:				# else if p1down
	LOAD	V4, $D		# D key = player 1 down
	SKPR	V4
	JUMP	p2up		# this is skipped when down is pressed
	SKE		VB, $1B		# don't move if y value is 27
	ADDR	VB, V1		# VB = VB + V1 (V1 = 1)
	JUMP	p2up

	p2up:				# p2up
	LOAD	V4, $1		# 1 key = player 2 up
	SKPR	V4
	JUMP	p2down		# this is skipped if up is pressed	
	SKE		VD, $0		# don't move if y value is 0
	SUB		VD, V1		# VD = VD - V1 (V1 = 1)
	JUMP	p2down

	p2down:				# p2down
	LOAD	V4, $4		# 4 key = player 1 down
	SKPR	V4
	JUMP	endupdate	# this is skipped when down is pressed
	SKE		VD, $1B		# don't move if y value is 27
	ADDR	VD, V1		# VB = VB + V1 (V1 = 1)
	JUMP	endupdate

	endupdate:
	CALL	resettimer
	RTS

resettimer:
	LOAD	V0, $1		# 60 update cycles per second
	LOADD	V0
	RTS

drawplayers:
	LOADI	$100
	DRAW	VA, VB, $5	# draw player 1
	DRAW	VC, VD, $5	# draw player 2
	RTS

drawball:
	LOADI	$100
	DRAW	V8, V9, $1
	# check for collision
	SKE		VF, $1
	JUMP	enddrawball

	# collision has happened
	SKE		V6, $1
	JUMP	ballgoright
	# ball go left
	LOAD	V6, $0
	JUMP	enddrawball

	ballgoright:
	LOAD	V6, $1

	enddrawball:
	RTS

updateball:				# handle ball movement
	# right
	SKE		V6, $1
	JUMP	ballleft
	ADD		V8, $1
	JUMP	ballup

	ballleft:
	SUB		V8, V1		# subtract 1

	ballup:
	SKE		V7, $1
	JUMP	balldown
	SUB		V9, V1		# subtract 1
	JUMP	endupdateball

	balldown:
	ADD		V9, $1

	endupdateball:
	# check for upper boundary collisions
	SKE		V9, $0
	JUMP	ballhitdown
	LOAD	V7, $0
	JUMP	ballhitleft

	ballhitdown:
	SKE		V9, $1F
	JUMP	ballhitleft
	LOAD	V7, $1

	ballhitleft:
	SKE		V8, $0
	JUMP	ballhitright
	ADD		V5, $1			# increase player 2 score
	CALL	resetball
	JUMP	endballcollision

	ballhitright:
	SKE		V8, $3F
	JUMP	endballcollision
	ADD		V3, $1			# increase player 1 score
	CALL	resetball

	endballcollision:
	RTS	

resetball:
	# position
	LOAD	V8, $1F
	LOAD	V9, $F
	# velocity
	LOAD	V6, $1
	LOAD	V7, $1
	RTS

resetscore:
	LOAD	V3, $0		# player 1 score
	LOAD	V5, $0		# player 2 score
	RTS

drawscore:
	LOAD	V0, $4
	LDSPR	V3
	DRAW	V0, V1, $5
	LOAD	V0, $37
	LDSPR	V5
	DRAW	V0, V1, $5
	RTS

checkwinner:
	SKE		V3, $3
	JUMP	p2winner
	CALL	resetscore
	CALL	resetball
	CALL	resetplayers
	RTS
	p2winner:
	SKE		V5, $3
	RTS
	CALL	resetscore
	CALL	resetball
	CALL	resetplayers
	RTS

resetplayers:
	LOAD	VA, $2		# player 1 x
	LOAD	VB, $C		# player 1 y
	LOAD	VC, $3C		# player 2 x
	LOAD	VD, $C		# player 2 y
	RTS