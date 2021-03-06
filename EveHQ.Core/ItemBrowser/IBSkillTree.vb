﻿'==============================================================================
'
' EveHQ - An Eve-Online™ character assistance application
' Copyright © 2005-2015  EveHQ Development Team
'
' This file is part of EveHQ.
'
' The source code for EveHQ is free and you may redistribute 
' it and/or modify it under the terms of the MIT License. 
'
' Refer to the NOTICES file in the root folder of EVEHQ source
' project for details of 3rd party components that are covered
' under their own, separate licenses.
'
' EveHQ is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
' license below for details.
'
' ------------------------------------------------------------------------------
'
' The MIT License (MIT)
'
' Copyright © 2005-2015  EveHQ Development Team
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.
'
' ==============================================================================

Imports DevComponents.AdvTree
Imports DevComponents.DotNetBar
Imports System.Drawing

Namespace ItemBrowser

    Public Class IBSkillTree

        Public Sub New()

            ' This call is required by the designer.
            InitializeComponent()

            _trainedStyle = adtSkills.Styles("BasicStyle").Copy
            _notTrainedStyle = adtSkills.Styles("BasicStyle").Copy
            _fullyPlannedStyle = adtSkills.Styles("BasicStyle").Copy
            _partialPlannedStyle = adtSkills.Styles("BasicStyle").Copy

            _trainedStyle.TextColor = Color.LimeGreen
            _notTrainedStyle.TextColor = Color.Red
            _fullyPlannedStyle.TextColor = Color.Blue
            _partialPlannedStyle.TextColor = Color.Orange

        End Sub

#Region "Public Properties"

        Public Property ItemIsUsable As Boolean
        Public Property ItemUsableTime As Long
        Public Property RequiredSkills As New Dictionary(Of String, Integer) ' skillID, skillLevel

#End Region

#Region "Class Variables"

        ReadOnly _trainedStyle As ElementStyle
        ReadOnly _notTrainedStyle As ElementStyle
        ReadOnly _fullyPlannedStyle As ElementStyle
        ReadOnly _partialPlannedStyle As ElementStyle

#End Region

        Public Sub DisplaySkills(typeID As Integer, itemSkills As Dictionary(Of Integer, Integer), skillsPilot As EveHQPilot)

            ' Set the ItemIsUsable flag and time, and reset the required skills
            ItemIsUsable = True
            ItemUsableTime = 0
            RequiredSkills.Clear()

            adtSkills.BeginUpdate()
            adtSkills.Nodes.Clear()

            For Each skillID As Integer In itemSkills.Keys

                If HQ.SkillListID.ContainsKey(skillID) Then

                    ' Get this skill
                    Dim curSkill As EveSkill = HQ.SkillListID(skillID)

                    ' Add the skill to the node list
                    Dim skillNode As New Node
                    skillNode.Name = skillID.ToString
                    skillNode.Text = curSkill.Name & " (Level " & itemSkills(skillID).ToString & ")"
                    CheckPilotSkillStatus(skillNode, skillsPilot, curSkill, itemSkills(skillID))
                    adtSkills.Nodes.Add(skillNode)

                    ' Add any pre-reqs of this skill
                    Call DisplaySkillPreReqs(skillsPilot, curSkill, skillNode)

                End If

            Next

            adtSkills.EndUpdate()
            adtSkills.ExpandAll()

            ' Calculate usable time
            For Each skillName As String In RequiredSkills.Keys
                ItemUsableTime += SkillFunctions.CalcTimeToLevel(skillsPilot, HQ.SkillListName(skillName), RequiredSkills(skillName))
            Next

        End Sub

        Private Sub DisplaySkillPreReqs(skillsPilot As EveHQPilot, parentSkill As EveSkill, parentNode As Node)

            For Each skillID As Integer In parentSkill.PreReqSkills.Keys

                If skillID <> parentSkill.ID Then ' Catch the recursive skills for Devs/GMs

                    ' Get this skill
                    Dim curSkill As EveSkill = HQ.SkillListID(skillID)

                    ' Add the skill to the node list
                    Dim skillNode As New Node
                    skillNode.Name = skillID.ToString
                    skillNode.Text = curSkill.Name & " (Level " & parentSkill.PreReqSkills(skillID).ToString & ")"
                    CheckPilotSkillStatus(skillNode, skillsPilot, curSkill, parentSkill.PreReqSkills(skillID))
                    parentNode.Nodes.Add(skillNode)

                    ' Add any pre-reqs of this skill
                    If parentSkill.PreReqSkills.Count > 0 Then
                        Call DisplaySkillPreReqs(skillsPilot, curSkill, skillNode)
                    End If

                End If

            Next

        End Sub

        Private Sub CheckPilotSkillStatus(skillNode As Node, skillsPilot As EveHQPilot, skill As EveSkill, skillLevel As Integer)

            If skillsPilot IsNot Nothing Then
                If SkillFunctions.IsSkillTrained(skillsPilot, skill.Name, skillLevel) Then
                    skillNode.Style = _trainedStyle
                    STTSkill.SetSuperTooltip(skillNode, New SuperTooltipInfo("Skill Information", skill.Name & " " & SkillFunctions.Roman(skillLevel), "This skill has already been trained by " & skillsPilot.Name & ".", My.Resources.SkillBookClosed32, My.Resources.Info32, eTooltipColor.Yellow))
                Else
                    ItemIsUsable = False
                    If RequiredSkills.ContainsKey(skill.Name) = False Then
                        RequiredSkills.Add(skill.Name, skillLevel)
                    Else
                        If skillLevel > RequiredSkills(skill.Name) Then
                            RequiredSkills(skill.Name) = skillLevel
                        End If
                    End If
                    Dim planLevel As Integer = SkillQueueFunctions.IsPlanned(skillsPilot, skill.Name, skillLevel)
                    If planLevel = 0 Then ' Not in the skill plan
                        skillNode.Style = _notTrainedStyle
                        STTSkill.SetSuperTooltip(skillNode, New SuperTooltipInfo("Skill Information", skill.Name & " " & SkillFunctions.Roman(skillLevel), "This skill is not trained and is not currently in any skill queue for " & skillsPilot.Name & ".", My.Resources.SkillBook32, My.Resources.Info32, eTooltipColor.Yellow))
                    Else
                        STTSkill.SetSuperTooltip(skillNode, New SuperTooltipInfo("Skill Information", skill.Name & " " & SkillFunctions.Roman(skillLevel), "This skill is not trained but is currently in a skill queue to be trained to level " & planLevel.ToString & ".", My.Resources.SkillBook32, My.Resources.Info32, eTooltipColor.Yellow))
                        If planLevel >= skillLevel Then ' Planned to train past the required level
                            skillNode.Style = _fullyPlannedStyle
                        Else ' Not planned to train to the required level
                            skillNode.Style = _partialPlannedStyle
                        End If
                    End If
                End If
            End If

        End Sub

    End Class
End NameSpace