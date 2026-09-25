cmake_minimum_required(VERSION 3.10)
project(greenshot-proxy VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Static runtime linking to avoid MSVCRT dependencies
if(MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

add_executable(greenshot-proxy WIN32 greenshot-proxy.cpp)

# Link required Windows libraries
target_link_libraries(greenshot-proxy 
    PRIVATE 
    shlwapi 
    advapi32 
    shell32 
    ole32 
    oleaut32 
    user32
)
