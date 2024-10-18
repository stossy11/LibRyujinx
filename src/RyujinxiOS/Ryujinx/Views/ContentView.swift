//
//  ContentView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct ContentView: View {
    private let settings: Settings = Settings()
    private let games: Games = Games()

    var body: some View {
        TabView {
            LibraryView()
                .environmentObject(settings)
                .environmentObject(games)
                .tabItem {
                    Label("Library", systemImage: "square.grid.2x2")
                }
            SettingsView()
                .environmentObject(settings)
                .tabItem {
                    Label("Settings", systemImage: "gearshape")
                }
            Text("")
                .tabItem {
                    Label("Profile", systemImage: "person.circle")
                }
        }
    }
}

#Preview {
    ContentView()
}
