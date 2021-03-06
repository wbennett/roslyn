﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Module Extensions
    <Extension()>
    Public Function GetReferencedAssemblySymbol(compilation As Compilation, reference As MetadataReference) As AssemblySymbol
        Return DirectCast(compilation.GetAssemblyOrModuleSymbol(reference), AssemblySymbol)
    End Function

    <Extension()>
    Public Function GetReferencedModuleSymbol(compilation As Compilation, reference As MetadataReference) As ModuleSymbol
        Return DirectCast(compilation.GetAssemblyOrModuleSymbol(reference), ModuleSymbol)
    End Function

    <Extension()>
    Public Function ToTestDisplayString(Symbol As ISymbol) As String
        Return Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat)
    End Function

    Private Function SplitMemberName(name As String) As ImmutableArray(Of String)
        Dim builder = ArrayBuilder(Of String).GetInstance()
        While True
            Dim separator = name.IndexOf("."c)
            If separator < 0 Then
                Exit While
            End If
            builder.Add(name.Substring(0, separator))
            name = name.Substring(separator + 1)
        End While
        builder.Add(name)
        Return builder.ToImmutableAndFree()
    End Function

    <Extension()>
    Public Function GetMember(comp As Compilation, name As String) As Symbol
        Return DirectCast(comp, VisualBasicCompilation).GlobalNamespace.GetMember(name)
    End Function

    <Extension()>
    Public Function GetMember(Of T As Symbol)(comp As Compilation, name As String) As T
        Return DirectCast(DirectCast(comp, VisualBasicCompilation).GlobalNamespace.GetMember(name), T)
    End Function

    <Extension()>
    Public Function GetMembers(comp As Compilation, name As String) As ImmutableArray(Of Symbol)
        Return GetMembers(DirectCast(comp, VisualBasicCompilation).GlobalNamespace, name)
    End Function

    Private Function GetMembers([namespace] As NamespaceSymbol, name As String) As ImmutableArray(Of Symbol)
        Dim parts = SplitMemberName(name)
        Dim symbol As NamespaceOrTypeSymbol = [namespace]
        For i = 0 To parts.Length - 2
            symbol = DirectCast(symbol.GetMember(parts(i)), NamespaceOrTypeSymbol)
        Next
        Return symbol.GetMembers(parts(parts.Length - 1))
    End Function

    <Extension()>
    Public Function GetMember([namespace] As NamespaceSymbol, name As String) As Symbol
        Return GetMembers([namespace], name).Single()
    End Function

    <Extension()>
    Public Function GetMember(symbol As NamespaceOrTypeSymbol, name As String) As Symbol
        Return symbol.GetMembers(name).Single()
    End Function

    <Extension()>
    Public Function GetMember(Of T As Symbol)(symbol As NamespaceOrTypeSymbol, name As String) As T
        Return DirectCast(symbol.GetMember(name), T)
    End Function

    <Extension()>
    Friend Function GetTypeMember(this As NamespaceOrTypeSymbol, name As String) As NamedTypeSymbol
        Return this.GetTypeMembers(name).Single
    End Function

    <Extension()>
    Friend Function GetNamespace(this As NamespaceSymbol, name As String) As NamespaceSymbol
        Return DirectCast(this.GetMembers(name).Single(), NamespaceSymbol)
    End Function

    <Extension()>
    Friend Function GetAttribute(this As Symbol, c As NamedTypeSymbol) As VisualBasicAttributeData
        Return this.GetAttributes().Where(Function(a) a.AttributeClass = c).First()
    End Function

    <Extension()>
    Friend Function GetAttribute(this As Symbol, m As MethodSymbol) As VisualBasicAttributeData
        Return this.GetAttributes().Where(Function(a) a.AttributeConstructor = m).First()
    End Function

    <Extension()>
    Friend Function GetAttributes(this As Symbol, c As NamedTypeSymbol) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.AttributeClass = c)
    End Function

    <Extension()>
    Friend Function GetAttributes(this As Symbol, m As MethodSymbol) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.AttributeConstructor = m)
    End Function

    <Extension()>
    Friend Function GetAttributes(this As Symbol, namespaceName As String, typeName As String) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.IsTargetAttribute(namespaceName, typeName))
    End Function

    <Extension()>
    Friend Function GetAttributes(this As Symbol, description As AttributeDescription) As IEnumerable(Of VisualBasicAttributeData)
        Return this.GetAttributes().Where(Function(a) a.IsTargetAttribute(this, description))
    End Function

    <Extension()>
    Friend Sub VerifyValue(Of T)(ByVal attr As VisualBasicAttributeData, ByVal i As Integer, ByVal kind As TypedConstantKind, ByVal v As T)
        Dim arg = attr.CommonConstructorArguments(i)
        Assert.Equal(kind, arg.Kind)
        Assert.True(IsEqual(Of T)(arg, v))
    End Sub

    <Extension()>
    Friend Sub VerifyValue(Of T)(ByVal attr As VisualBasicAttributeData, ByVal i As Integer, ByVal name As String, ByVal kind As TypedConstantKind, ByVal v As T)
        Dim namedArg = attr.CommonNamedArguments(i)
        Assert.Equal(namedArg.Key, name)
        Dim arg = namedArg.Value
        Assert.Equal(arg.Kind, kind)
        Assert.True(IsEqual(Of T)(arg, v))
    End Sub

    <Extension()>
    Friend Sub VerifyNamedArgumentValue(Of T)(ByVal attr As VisualBasicAttributeData, i As Integer, name As String, kind As TypedConstantKind, v As T)
        Dim namedArg = attr.CommonNamedArguments(i)
        Assert.Equal(namedArg.Key, name)
        Dim arg = namedArg.Value
        Assert.Equal(arg.Kind, kind)
        Assert.True(IsEqual(arg, v))
    End Sub

    Private Function IsEqual(Of T)(ByVal arg As TypedConstant, ByVal expected As T) As Boolean

        Select Case arg.Kind
            Case TypedConstantKind.Array
                Return AreEqual(arg.Values, expected)
            Case TypedConstantKind.Enum
                Return expected.Equals(arg.Value)
            Case TypedConstantKind.Type
                Dim typeSym = TryCast(arg.Value, TypeSymbol)
                If typeSym Is Nothing Then
                    Return False
                End If

                Dim expTypeSym = TryCast(expected, TypeSymbol)
                If typeSym.Equals(expTypeSym) Then
                    Return True
                End If

                ' TODO: improve the comparison mechanism for generic types.
                If typeSym.Kind = SymbolKind.NamedType AndAlso
                    DirectCast(typeSym, NamedTypeSymbol).IsGenericType() Then

                    Dim s1 = typeSym.ToDisplayString(SymbolDisplayFormat.TestFormat)
                    Dim s2 = expected.ToString()
                    If (s1 = s2) Then
                        Return True
                    End If

                End If

                Dim expType = TryCast(expected, Type)
                If expType Is Nothing Then
                    Return False
                End If
                'Can't always simply compare string as <T>.ToString() is IL format
                Return IsEqual(typeSym, expType)
            Case Else
                'Assert.Equal(expected, CType(arg.Value, T))
                Return If(expected Is Nothing, arg.Value Is Nothing, expected.Equals(CType(arg.Value, T)))
        End Select

    End Function

    ''' For argument is not simple 'Type' (generic or array)
    Private Function IsEqual(typeSym As TypeSymbol, expType As Type) As Boolean
        '' namedType
        Dim typeSymTypeKind As TypeKind = typeSym.TypeKind
        If typeSymTypeKind = TypeKind.Interface OrElse typeSymTypeKind = TypeKind.Class OrElse
            typeSymTypeKind = TypeKind.Structure OrElse typeSymTypeKind = TypeKind.Delegate Then

            Dim namedType = DirectCast(typeSym, NamedTypeSymbol)
            ' name should be same if it's not generic (NO ByRef in attribute)
            If (namedType.Arity = 0) Then
                Return typeSym.Name = expType.Name
            End If
            ' generic
            If Not (expType.IsGenericType) Then
                Return False
            End If

            Dim nameOnly = expType.Name
            'generic <Name>'1
            Dim idx = expType.Name.LastIndexOfAny(New Char() {"`"c})
            If (idx > 0) Then
                nameOnly = expType.Name.Substring(0, idx)
            End If
            If Not (typeSym.Name = nameOnly) Then
                Return False
            End If
            Dim expArgs = expType.GetGenericArguments()
            Dim actArgs = namedType.TypeArguments()
            If Not (expArgs.Count = actArgs.Length) Then
                Return False
            End If

            For i = 0 To expArgs.Count - 1
                If Not IsEqual(actArgs(i), expArgs(i)) Then
                    Return False
                End If
            Next
            Return True
            ' array type
        ElseIf typeSymTypeKind = TypeKind.ArrayType Then
            If Not expType.IsArray Then
                Return False
            End If
            Dim arySym = DirectCast(typeSym, ArrayTypeSymbol)
            If Not IsEqual(arySym.ElementType, expType.GetElementType()) Then
                Return False
            End If
            If Not IsEqual(arySym.BaseType, expType.BaseType) Then
                Return False
            End If
            Return arySym.Rank = expType.GetArrayRank()
        End If

        Return False
    End Function

    ' Compare an Object with a TypedConstant.  This compares the TypeConstant's value and ignores the TypeConstant's type.
    Private Function AreEqual(tc As ImmutableArray(Of TypedConstant), o As Object) As Boolean

        If o Is Nothing Then
            Return tc.IsDefault
        ElseIf tc.IsDefault Then
            Return False
        End If

        If Not o.GetType.IsArray Then
            Return False
        End If

        Dim a = DirectCast(o, Array)
        Dim ret As Boolean = True
        For i = 0 To a.Length - 1
            Dim v = a.GetValue(i)
            Dim c = tc(i)
            ret = ret And IsEqual(c, v)
        Next
        Return ret
    End Function

End Module