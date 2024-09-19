import os

def count_cs_lines_in_project(root_dir='.'):
    total_code_lines = 0
    total_excluded_lines = 0
    total_lines = 0
    cs_files_counted = 0
    file_line_counts = []

    # Directories to skip
    excluded_dirs = ['Migrations', 'obj', 'Properties', 'bin', '.godot']

    # Walk through the directory recursively
    for subdir, dirs, files in os.walk(root_dir):
        # Skip excluded directories
        if any(excluded_dir in subdir for excluded_dir in excluded_dirs):
            continue

        for file in files:
            if file.endswith('.cs'):
                cs_file_path = os.path.join(subdir, file)
                with open(cs_file_path, 'r', encoding='utf-8') as cs_file:
                    lines = cs_file.readlines()

                    # Classify lines into code lines and excluded lines (comments and empty)
                    code_lines = [line for line in lines if line.strip() and not line.strip().startswith('//')]
                    excluded_lines = [line for line in lines if not line.strip() or line.strip().startswith('//')]

                    code_line_count = len(code_lines)
                    excluded_line_count = len(excluded_lines)
                    total_line_count = len(lines)  # Total lines = code lines + excluded lines

                    total_code_lines += code_line_count
                    total_excluded_lines += excluded_line_count
                    total_lines += total_line_count
                    cs_files_counted += 1

                    # Store file details including code, excluded, and total lines
                    file_line_counts.append((cs_file_path, code_line_count, excluded_line_count, total_line_count))

    return file_line_counts, total_code_lines, total_excluded_lines, total_lines, cs_files_counted

if __name__ == "__main__":
    project_root = '.'  # Assuming the script is in the root of the project
    file_line_counts, total_code_lines, total_excluded_lines, total_lines, cs_files_counted = count_cs_lines_in_project(project_root)
    
    # Sort the files by code line count in descending order
    file_line_counts.sort(key=lambda x: x[1], reverse=True)

    # Determine the longest file name for consistent formatting
    max_path_length = max(len(file_path) for file_path, _, _, _ in file_line_counts)

    # Print the file line counts with better formatting
    print(f"{'File Path'.ljust(max_path_length)} | {'Code Lines'} | {'Excluded Lines'} | {'Total Lines'}")
    print('-' * (max_path_length + 45))  # separator line for readability

    for file_path, code_line_count, excluded_line_count, total_line_count in file_line_counts:
        print(f"{file_path.ljust(max_path_length)} | {str(code_line_count).ljust(11)} | {str(excluded_line_count).ljust(14)} | {total_line_count}")

    # Print the total counts at the end
    print(f"\nTotal .cs files counted: {cs_files_counted}")
    print(f"Total code lines in .cs files: {total_code_lines}")
    print(f"Total excluded lines (comments and empty lines) in .cs files: {total_excluded_lines}")
    print(f"Total lines in .cs files: {total_lines}")
