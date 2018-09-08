#include <gtest/gtest.h>
#include "../src/exec.h"

using namespace shanghai;
using namespace std::string_literals;

TEST(ExecTest, Simple) {
	Process p("/bin/uname"s, {});
	EXPECT_EQ(0, p.WaitForExit());
}

TEST(ExecTest, StdInOut) {
	const auto teststr = "hello, shanghai\n"s;

	Process p("/bin/cat"s, {});
	p.InputAndClose(teststr);
	EXPECT_EQ(0, p.WaitForExit());
	EXPECT_EQ(teststr, p.GetOut());
}

TEST(ExecTest, Timeout) {
	Process p("/bin/cat"s, {});
	EXPECT_THROW(p.WaitForExit(1), ProcessError);
}
