module AvaloniaFileSelect.FileSelect

open System.IO
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Platform.Storage
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL

[<RequireQualifiedAccess>]
type Scenario =
    | Baseline
    | Expand of selectedText: string option * excludedOptions: string list

type State =
    {
        Scenario: Scenario
        InputFile: string option
        Options: string list
    }
    static member init =
        {
            Scenario = Scenario.Baseline
            InputFile = None
            Options = []
        }

let handleFileSelect (window: Window, state: IWritable<State>) =
    async {
        let options =
            FilePickerOpenOptions(
                AllowMultiple = false,
                FileTypeFilter = [ FilePickerFileType("Excel Files", Patterns = [ "*.txt" ]) ]
            )

        let! files = window.StorageProvider.OpenFilePickerAsync(options) |> Async.AwaitTask
        if files.Count > 0 then
            // There should only be one file
            if files.Count > 1 then
                raise (System.ArgumentException "Cannot select multiple input files")

            let file = files[0]

            let options =
                File.ReadAllLines file.Path.LocalPath
                |> List.ofArray

            state.Set { state.Current with
                            Scenario = Scenario.Baseline
                            InputFile = Some file.Path.LocalPath
                            Options = options }

    }
    |> Async.Start

let view (window: Window) =

    Component(fun ctx ->
        let state = ctx.useState State.init
        let logs = ctx.useState List.empty<string>

        Border.create [
            Border.borderThickness 5.0
            Border.child
                (StackPanel.create [
                    StackPanel.spacing 10.0
                    StackPanel.verticalAlignment VerticalAlignment.Top
                    StackPanel.horizontalAlignment HorizontalAlignment.Left
                    StackPanel.children [
                        Button.create [
                            Button.content "Select Input file"
                            // async work can be performed inline or deferred to another function
                            Button.onClick (fun _ -> handleFileSelect (window, state))
                        ]
                        match state.Current.InputFile with
                        | None ->
                            TextBlock.create [ TextBlock.text "Please select input file to continue" ]

                        | Some inputFile ->
                            TextBlock.create [ TextBlock.text $"""Selected: {inputFile}""" ]
                            TextBlock.create [ TextBlock.text "Scenario Selection" ]
                            match state.Current.Scenario with
                            | Scenario.Baseline ->
                                CheckBox.create [
                                    CheckBox.content "Baseline"
                                    CheckBox.isChecked true
                                    CheckBox.onClick (fun _ ->
                                        let newState =
                                            { state.Current with
                                                Scenario = Scenario.Baseline }
                                        state.Set newState)
                                ]

                                CheckBox.create [
                                    CheckBox.content "Expand"
                                    CheckBox.isChecked false
                                    CheckBox.onClick (fun _ ->
                                        let newState =
                                            { state.Current with
                                                Scenario = Scenario.Expand (None, []) }
                                        state.Set newState)
                                ]

                            | Scenario.Expand (selectedOption, excludedOptions) ->
                                CheckBox.create [
                                    CheckBox.content "Baseline"
                                    CheckBox.isChecked false
                                    CheckBox.onClick (fun _ ->
                                        let newState =
                                            { state.Current with
                                                Scenario = Scenario.Baseline }
                                        state.Set newState)
                                ]
                                CheckBox.create [
                                    CheckBox.content "Expand"
                                    CheckBox.isChecked true
                                    CheckBox.onClick (fun _ ->
                                        let newState =
                                            { state.Current with
                                                Scenario = Scenario.Expand (None, []) }
                                        state.Set newState)
                                ]

                                TextBlock.create [ TextBlock.text "Expand Selection" ]

                                match selectedOption with
                                | None ->
                                    for op in state.Current.Options do
                                        CheckBox.create [
                                            CheckBox.content op
                                            CheckBox.isChecked false
                                            CheckBox.onClick (fun _ ->
                                                let newState =
                                                    { state.Current with
                                                        Scenario = Scenario.Expand (Some op, []) }
                                                state.Set newState)
                                        ]

                                | Some selectedOption ->
                                    CheckBox.create [
                                        CheckBox.content selectedOption
                                        CheckBox.isChecked true
                                        CheckBox.onClick (fun _ ->
                                            let newState =
                                                { state.Current with
                                                    Scenario = Scenario.Expand (None, []) }
                                            state.Set newState)
                                    ]
                                    TextBlock.create [ TextBlock.text "Exclude Selection" ]
                                    let nonSelectedOptions =
                                        state.Current.Options
                                        |> List.filter (fun d -> not (d.Equals selectedOption))

                                    for nonSelectedOption in nonSelectedOptions do
                                        let isSelected = List.contains nonSelectedOption excludedOptions
                                        CheckBox.create [
                                            CheckBox.content nonSelectedOption
                                            CheckBox.isChecked isSelected
                                            CheckBox.onClick (fun _ ->
                                                let newExcludedOptions =
                                                    if isSelected then
                                                        excludedOptions
                                                        |> List.filter (fun r -> r <> nonSelectedOption)
                                                    else
                                                        nonSelectedOption :: excludedOptions
                                                let newState =
                                                    { state.Current with
                                                        Scenario = Scenario.Expand (Some selectedOption, newExcludedOptions) }
                                                state.Set newState)
                                        ]

                            Button.create [
                                Button.content "Run"
                            ]
                            for log in logs.Current do
                                TextBlock.create [ TextBlock.text log ]
                    ]
                ])
        ])
